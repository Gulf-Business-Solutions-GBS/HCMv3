import { Component, ElementRef, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { LocalizationService } from 'abp-ng2-module';
import { AppConsts } from '@shared/AppConsts';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { parseTsv, toTsv } from './tsv';
import { validateCell } from './paste-grid.coercion';
import { CellIssue, CellRef, PasteGridColumn, PasteGridIssue, PasteGridRow, isRowEmpty } from './paste-grid.types';

/**
 * Spreadsheet-style grid for the Go-Live Setup's bulk sheets (employees, salaries, bank
 * details, opening balances).
 *
 * Why this exists rather than a PrimeNG table: p-table is built to display server-paged data. These
 * sheets are client-side, edited in place, and above all pasted into from Excel -- and clipboard range
 * paste is the one thing p-table's cell editing does not do. Everything else here (virtual scroll,
 * keyboard navigation, per-cell validation) exists to make paste usable at a few thousand rows.
 *
 * Rendering notes:
 *  - CSS grid rather than a <table>, so the CDK viewport can virtualise rows while the header stays
 *    aligned; header and body share one grid-template-columns and one horizontally scrolling parent.
 *  - Cells render as text. Exactly one editor input is mounted, over the active cell, so the DOM stays
 *    small and focus does not thrash while arrowing around.
 *
 * Member order below is dictated by @typescript-eslint/member-ordering (private fields, public fields,
 * constructor, public methods, private methods), not by feature grouping.
 *
 * Not yet done, deliberately -- see the notes in the demo harness: drag-to-select, column resize,
 * multi-level redo, and a fill handle.
 */
@Component({
    selector: 'app-paste-grid',
    templateUrl: './paste-grid.component.html',
    imports: [ScrollingModule, LocalizePipe],
    styleUrl: './paste-grid.component.css',
})
export class PasteGridComponent {
    private readonly host: ElementRef<HTMLElement> = inject(ElementRef);
    private readonly localization = inject(LocalizationService);
    private readonly source = AppConsts.localization.defaultLocalizationSourceName;

    /** Keyed `${rowIndex}|${columnKey}`. */
    private readonly issues = signal(new Map<string, CellIssue>());
    private readonly undoDepth = signal(0);
    private undoStack: PasteGridRow[][] = [];

    readonly columns = input.required<PasteGridColumn[]>();

    /** Two-way. The caller owns the data; the grid replaces the array on every committed change. */
    readonly rows = model<PasteGridRow[]>([]);

    /** Blank rows kept below the last filled one, so there is always somewhere to paste. */
    readonly padRows = input(30);

    /** Hard ceiling. A paste larger than this is truncated and the user is told. */
    readonly maxRows = input(5000);

    readonly viewportHeight = input(460);

    readonly issuesChange = output<PasteGridIssue[]>();

    readonly rowHeight = 30;

    readonly active = signal<CellRef>({ r: 0, c: 0 });
    readonly selectionEnd = signal<CellRef | null>(null);
    readonly editing = signal<CellRef | null>(null);
    readonly editValue = signal('');
    readonly hasFocus = signal(false);

    readonly issueCount = computed(() => this.issues().size);
    readonly canUndo = computed(() => this.undoDepth() > 0);
    readonly filledRowCount = computed(() => this.rows().filter((r) => !isRowEmpty(r)).length);

    readonly gridTemplate = computed(
        () =>
            `56px ${this.columns()
                .map((c) => `${c.width ?? 150}px`)
                .join(' ')}`
    );
    readonly totalWidth = computed(() => 56 + this.columns().reduce((sum, c) => sum + (c.width ?? 150), 0));

    /** Flattened for the summary strip; the caller gets the same list through issuesChange. */
    readonly issueList = computed<PasteGridIssue[]>(() => {
        const cols = new Map(this.columns().map((c) => [c.key, c]));
        const out: PasteGridIssue[] = [];
        for (const [composite, issue] of this.issues()) {
            const cut = composite.indexOf('|');
            const row = +composite.slice(0, cut);
            const key = composite.slice(cut + 1);
            out.push({ row, key, header: cols.get(key)?.header ?? key, issue });
        }
        return out.sort((a, b) => a.row - b.row);
    });

    readonly visibleIssues = computed(() => this.issueList().slice(0, 6));

    constructor() {
        // Keep a run of blank rows below the data so there is always a paste target.
        effect(() => {
            const rows = this.rows();
            const pad = this.padRows();
            let lastFilled = -1;
            for (let i = rows.length - 1; i >= 0; i--) {
                if (!isRowEmpty(rows[i])) {
                    lastFilled = i;
                    break;
                }
            }
            const wanted = Math.min(this.maxRows(), lastFilled + 1 + pad);
            if (rows.length < wanted) {
                this.rows.set([...rows, ...this.blankRows(wanted - rows.length)]);
            }
        });

        effect(() => {
            if (this.editing()) {
                // The editor is created by the same change detection pass that set this signal.
                setTimeout(() => {
                    const el = this.host.nativeElement.querySelector<HTMLElement>('.pg-input');
                    el?.focus();
                    if (el instanceof HTMLInputElement) {
                        el.select();
                    }
                }, 0);
            }
        });
    }

    // ---------------------------------------------------------------- rendering helpers

    /** Rows are positional, so the index is the identity. A method, not an arrow field, because
     *  member-ordering classifies an arrow field as a method and would want it after the constructor. */
    trackByIndex(index: number): number {
        return index;
    }

    display(row: PasteGridRow, col: PasteGridColumn): string {
        const v = row[col.key];
        return v === null || v === undefined ? '' : String(v);
    }

    isEditing(r: number, c: number): boolean {
        const e = this.editing();
        return !!e && e.r === r && e.c === c;
    }

    hasIssue(r: number, key: string): boolean {
        return this.issues().has(`${r}|${key}`);
    }

    issueText(r: number, key: string): string {
        const issue = this.issues().get(`${r}|${key}`);
        return issue ? this.l(issue.key, ...(issue.params ?? [])) : '';
    }

    inSelection(r: number, c: number): boolean {
        const rect = this.selectionRect();
        return r >= rect.r1 && r <= rect.r2 && c >= rect.c1 && c <= rect.c2;
    }

    // ---------------------------------------------------------------- mouse and keyboard

    onCellMouseDown(event: MouseEvent, r: number, c: number): void {
        if (this.editing()) {
            this.commitEdit();
        }
        if (event.shiftKey) {
            this.selectionEnd.set({ r, c });
        } else {
            this.active.set({ r, c });
            this.selectionEnd.set(null);
        }
    }

    onKeyDown(event: KeyboardEvent): void {
        if (this.editing()) {
            return;
        }

        const key = event.key;

        if ((event.ctrlKey || event.metaKey) && key.toLowerCase() === 'z') {
            event.preventDefault();
            this.undo();
            return;
        }
        if ((event.ctrlKey || event.metaKey) && key.toLowerCase() === 'a') {
            event.preventDefault();
            this.active.set({ r: 0, c: 0 });
            this.selectionEnd.set({ r: this.rows().length - 1, c: this.columns().length - 1 });
            return;
        }
        // Let the browser raise the native copy/paste events.
        if ((event.ctrlKey || event.metaKey) && ['c', 'v', 'x'].includes(key.toLowerCase())) {
            return;
        }

        switch (key) {
            case 'ArrowUp':
                event.preventDefault();
                this.move(-1, 0, event.shiftKey);
                return;
            case 'ArrowDown':
                event.preventDefault();
                this.move(1, 0, event.shiftKey);
                return;
            case 'ArrowLeft':
                event.preventDefault();
                this.move(0, -1, event.shiftKey);
                return;
            case 'ArrowRight':
                event.preventDefault();
                this.move(0, 1, event.shiftKey);
                return;
            case 'Tab':
                event.preventDefault();
                this.move(0, event.shiftKey ? -1 : 1, false);
                return;
            case 'Enter':
            case 'F2':
                event.preventDefault();
                this.startEdit(this.active().r, this.active().c);
                return;
            case 'Delete':
            case 'Backspace':
                event.preventDefault();
                this.clearSelection();
                return;
            case 'Escape':
                this.selectionEnd.set(null);
                return;
        }

        // Typing over a cell replaces it, as in Excel.
        if (key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
            event.preventDefault();
            this.startEdit(this.active().r, this.active().c, key);
        }
    }

    // ---------------------------------------------------------------- editing

    startEdit(r: number, c: number, seed?: string): void {
        const col = this.columns()[c];
        if (!col || col.type === 'readonly') {
            return;
        }
        this.active.set({ r, c });
        this.selectionEnd.set(null);
        this.editValue.set(seed ?? this.display(this.rows()[r] ?? {}, col));
        this.editing.set({ r, c });
    }

    onEditorInput(event: Event): void {
        this.editValue.set((event.target as HTMLInputElement | HTMLSelectElement).value);
    }

    onEditorKeyDown(event: KeyboardEvent): void {
        if (event.key === 'Enter') {
            event.preventDefault();
            this.commitEdit();
            this.move(1, 0, false);
        } else if (event.key === 'Escape') {
            event.preventDefault();
            this.editing.set(null);
        } else if (event.key === 'Tab') {
            event.preventDefault();
            this.commitEdit();
            this.move(0, event.shiftKey ? -1 : 1, false);
        }
        event.stopPropagation();
    }

    commitEdit(): void {
        const cell = this.editing();
        if (!cell) {
            return;
        }
        const col = this.columns()[cell.c];
        this.editing.set(null);

        const current = this.rows()[cell.r]?.[col.key];
        const raw = this.editValue();
        if (String(current ?? '') === raw) {
            return;
        }

        this.pushUndo();
        const rows = [...this.rows()];
        rows[cell.r] = { ...rows[cell.r], [col.key]: raw };
        this.writeAndValidate(rows, cell.r, cell.r, cell.c, cell.c);
    }

    // ---------------------------------------------------------------- clipboard

    onPaste(event: ClipboardEvent): void {
        if (this.editing()) {
            return;
        }
        const text = event.clipboardData?.getData('text/plain') ?? '';
        event.preventDefault();
        if (!text) {
            return;
        }

        const parsed = parseTsv(text);
        const cols = this.columns();
        const start = this.active();
        const rect = this.selectionRect();

        this.pushUndo();
        const rows = [...this.rows()];

        // Excel behaviour: one value pasted over a range fills the whole range.
        const singleValue = parsed.length === 1 && parsed[0].length === 1;
        const rangeSelected = rect.r1 !== rect.r2 || rect.c1 !== rect.c2;

        if (singleValue && rangeSelected) {
            for (let r = rect.r1; r <= rect.r2; r++) {
                this.ensureRow(rows, r);
                const next = { ...rows[r] };
                for (let c = rect.c1; c <= rect.c2; c++) {
                    if (cols[c].type !== 'readonly') {
                        next[cols[c].key] = parsed[0][0];
                    }
                }
                rows[r] = next;
            }
            this.writeAndValidate(rows, rect.r1, rect.r2, rect.c1, rect.c2);
            return;
        }

        const lastRow = Math.min(start.r + parsed.length - 1, this.maxRows() - 1);
        const truncated = start.r + parsed.length > this.maxRows();
        let lastCol = start.c;

        for (let i = 0; start.r + i <= lastRow; i++) {
            const r = start.r + i;
            this.ensureRow(rows, r);
            const next = { ...rows[r] };
            const line = parsed[i];
            for (let j = 0; j < line.length; j++) {
                const c = start.c + j;
                if (c >= cols.length) {
                    break;
                }
                if (cols[c].type !== 'readonly') {
                    next[cols[c].key] = line[j];
                }
                lastCol = Math.max(lastCol, c);
            }
            rows[r] = next;
        }

        this.writeAndValidate(rows, start.r, lastRow, start.c, lastCol);
        this.active.set({ r: start.r, c: start.c });
        this.selectionEnd.set({ r: lastRow, c: lastCol });

        if (truncated) {
            abp.notify.warn(this.l('PasteGridTruncated', this.maxRows()));
        }
    }

    onCopy(event: ClipboardEvent): void {
        if (this.editing()) {
            return;
        }
        const rect = this.selectionRect();
        const cols = this.columns();
        const rows = this.rows();
        const out: string[][] = [];

        for (let r = rect.r1; r <= rect.r2; r++) {
            const line: string[] = [];
            for (let c = rect.c1; c <= rect.c2; c++) {
                line.push(this.display(rows[r] ?? {}, cols[c]));
            }
            out.push(line);
        }

        event.clipboardData?.setData('text/plain', toTsv(out));
        event.preventDefault();
    }

    // ---------------------------------------------------------------- mutations

    clearSelection(): void {
        const rect = this.selectionRect();
        const cols = this.columns();
        this.pushUndo();
        const rows = [...this.rows()];

        for (let r = rect.r1; r <= rect.r2 && r < rows.length; r++) {
            const next = { ...rows[r] };
            for (let c = rect.c1; c <= rect.c2; c++) {
                if (cols[c].type !== 'readonly') {
                    next[cols[c].key] = null;
                }
            }
            rows[r] = next;
        }
        this.writeAndValidate(rows, rect.r1, rect.r2, rect.c1, rect.c2);
    }

    clearAll(): void {
        this.pushUndo();
        this.rows.set(this.blankRows(this.padRows()));
        this.issues.set(new Map());
        this.active.set({ r: 0, c: 0 });
        this.selectionEnd.set(null);
        this.emitIssues();
    }

    addRows(n: number): void {
        this.rows.set([...this.rows(), ...this.blankRows(n)]);
    }

    undo(): void {
        const prev = this.undoStack.pop();
        this.undoDepth.set(this.undoStack.length);
        if (!prev) {
            return;
        }
        this.rows.set(prev);
        this.revalidateAll();
    }

    revalidateAll(): void {
        const cols = this.columns();
        const rows = this.rows().map((r) => ({ ...r }));
        const issues = new Map<string, CellIssue>();

        for (let r = 0; r < rows.length; r++) {
            const row = rows[r];
            if (isRowEmpty(row)) {
                continue;
            }
            for (const col of cols) {
                const { value, issue } = validateCell(col, row[col.key], row);
                row[col.key] = value;
                if (issue) {
                    issues.set(`${r}|${col.key}`, issue);
                }
            }
        }

        this.rows.set(rows);
        this.issues.set(issues);
        this.emitIssues();
    }

    goToIssue(issue: PasteGridIssue): void {
        const c = this.columns().findIndex((col) => col.key === issue.key);
        if (c < 0) {
            return;
        }
        this.active.set({ r: issue.row, c });
        this.selectionEnd.set(null);
        this.scrollRowIntoView(issue.row);
        this.host.nativeElement.querySelector<HTMLElement>('.cdk-virtual-scroll-viewport')?.focus();
    }

    /** Local equivalent of AppComponentBase.l() -- this component deliberately does not extend it. */
    l(key: string, ...args: unknown[]): string {
        let text = this.localization.localize(key, this.source);
        if (!text) {
            text = key;
        }
        if (!args.length) {
            return text;
        }
        return abp.utils.formatString.apply(this, [text, ...args] as never);
    }

    // ---------------------------------------------------------------- internals

    private selectionRect(): { r1: number; r2: number; c1: number; c2: number } {
        const a = this.active();
        const b = this.selectionEnd() ?? a;
        return {
            r1: Math.min(a.r, b.r),
            r2: Math.max(a.r, b.r),
            c1: Math.min(a.c, b.c),
            c2: Math.max(a.c, b.c),
        };
    }

    private move(dr: number, dc: number, extend: boolean): void {
        const base = extend ? (this.selectionEnd() ?? this.active()) : this.active();
        const r = Math.max(0, Math.min(this.rows().length - 1, base.r + dr));
        const c = Math.max(0, Math.min(this.columns().length - 1, base.c + dc));

        if (extend) {
            this.selectionEnd.set({ r, c });
        } else {
            this.active.set({ r, c });
            this.selectionEnd.set(null);
        }
        this.scrollRowIntoView(r);
    }

    private scrollRowIntoView(r: number): void {
        const viewport = this.host.nativeElement.querySelector<HTMLElement>('.cdk-virtual-scroll-viewport');
        if (!viewport) {
            return;
        }
        const top = r * this.rowHeight;
        const bottom = top + this.rowHeight;
        if (top < viewport.scrollTop) {
            viewport.scrollTop = top;
        } else if (bottom > viewport.scrollTop + viewport.clientHeight) {
            viewport.scrollTop = bottom - viewport.clientHeight;
        }
    }

    private ensureRow(rows: PasteGridRow[], index: number): void {
        while (rows.length <= index) {
            rows.push(this.blankRow());
        }
    }

    private blankRow(): PasteGridRow {
        const row: PasteGridRow = {};
        for (const col of this.columns()) {
            row[col.key] = null;
        }
        return row;
    }

    private blankRows(n: number): PasteGridRow[] {
        return Array.from({ length: n }, () => this.blankRow());
    }

    private pushUndo(): void {
        this.undoStack.push(this.rows().map((r) => ({ ...r })));
        if (this.undoStack.length > 20) {
            this.undoStack.shift();
        }
        this.undoDepth.set(this.undoStack.length);
    }

    /** Writes the new rows, then revalidates only the rectangle that changed. */
    private writeAndValidate(rows: PasteGridRow[], r1: number, r2: number, c1: number, c2: number): void {
        const cols = this.columns();
        const issues = new Map(this.issues());

        for (let r = r1; r <= r2 && r < rows.length; r++) {
            const row = rows[r];
            const empty = isRowEmpty(row);

            // A row that has just been emptied must drop the required-field issues on its other columns.
            if (empty) {
                for (const col of cols) {
                    issues.delete(`${r}|${col.key}`);
                }
                continue;
            }

            for (let c = c1; c <= c2 && c < cols.length; c++) {
                const col = cols[c];
                const composite = `${r}|${col.key}`;
                const { value, issue } = validateCell(col, row[col.key], row);
                row[col.key] = value;
                if (issue) {
                    issues.set(composite, issue);
                } else {
                    issues.delete(composite);
                }
            }
        }

        this.rows.set(rows);
        this.issues.set(issues);
        this.emitIssues();
    }

    private emitIssues(): void {
        this.issuesChange.emit(this.issueList());
    }
}
