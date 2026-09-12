import { Component, computed, inject, input, output, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/** A sheet of a spreadsheet, already read: the first row is its heading. */
export interface V2DocSheet {
    name: string;
    rows: (string | number | null)[][];
}

/**
 * A document Go-Live Setup V2 has produced, ready to look at before it is saved.
 *
 * <p>The bytes are the file itself -- what Download saves is exactly what was generated, never a
 * second rendering of the same data.</p>
 */
export interface V2Document {
    /** What produced it, e.g. "Payroll Sheet". */
    title: string;
    fileName: string;
    kind: 'pdf' | 'sheet' | 'text';
    blob: Blob;
    /** Object URL of the PDF, for the browser's own viewer. */
    pdfUrl?: string;
    sheets?: V2DocSheet[];
    text?: string;
}

/**
 * One window for everything V2 produces: the payslip PDF, the two Excel reports, and the bank file --
 * shown before anyone saves it, because a file you cannot see until it is on disk is a file nobody
 * checks.
 *
 * <p>A PDF goes into the browser's own viewer, which already has zoom, find, print and save; there is
 * no reason to rebuild those badly. A spreadsheet or a fixed-width text file is drawn here, with a find
 * box that keeps only the rows it matches and a text size the reader controls -- a bank file's columns
 * are unreadable shrunk, and a hundred payslip rows are unreadable large.</p>
 */
@Component({
    selector: 'v2-doc-viewer',
    templateUrl: './v2-doc-viewer.component.html',
    styleUrl: './v2-doc-viewer.component.css',
    imports: [LocalizePipe],
})
export class V2DocViewerComponent extends AppComponentBase {
    private readonly sanitizer = inject(DomSanitizer);

    readonly doc = input.required<V2Document>();
    readonly closed = output<void>();

    readonly find = signal('');
    readonly sheetIndex = signal(0);
    readonly textScale = signal(1);

    readonly safePdfUrl = computed<SafeResourceUrl | null>(() => {
        const url = this.doc().pdfUrl;
        return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
    });

    readonly sheet = computed<V2DocSheet | null>(() => this.doc().sheets?.[this.sheetIndex()] ?? null);

    /** The first row names the columns; everything below it is data. */
    readonly headings = computed<(string | number | null)[]>(() => this.sheet()?.rows?.[0] ?? []);

    readonly rows = computed<(string | number | null)[][]>(() => {
        const all = (this.sheet()?.rows ?? []).slice(1);
        const needle = this.find().trim().toLowerCase();
        return needle
            ? all.filter((row) => row.some((cell) => String(cell ?? '').toLowerCase().includes(needle)))
            : all;
    });

    readonly textLines = computed<string[]>(() => {
        const needle = this.find().trim().toLowerCase();
        const lines = (this.doc().text ?? '').split(/\r?\n/);
        return needle ? lines.filter((line) => line.toLowerCase().includes(needle)) : lines;
    });

    readonly rowCount = computed(() =>
        this.doc().kind === 'text' ? this.textLines().length : this.rows().length
    );

    onFind(event: Event): void {
        this.find.set((event.target as HTMLInputElement).value);
    }

    zoom(by: number): void {
        this.textScale.update((scale) => Math.min(2, Math.max(0.6, Math.round((scale + by) * 10) / 10)));
    }

    pickSheet(index: number): void {
        this.sheetIndex.set(index);
        this.find.set('');
    }

    download(): void {
        const doc = this.doc();
        const url = URL.createObjectURL(doc.blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = doc.fileName || 'download';
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 60000);
    }

    close(): void {
        this.closed.emit();
    }
}
