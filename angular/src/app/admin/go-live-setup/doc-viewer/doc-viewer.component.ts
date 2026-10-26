import { Component, computed, inject, input, output, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * A document Go-Live Setup has produced, ready to read before it is saved.
 *
 * <p>A report arrives as rows the server read out of the very file it generated, so the table and the
 * download cannot drift apart. A payslip arrives as the PDF itself.</p>
 */
export interface GoLiveDocument {
    /** What produced it, e.g. "Payroll Sheet". */
    title: string;
    kind: 'pdf' | 'grid' | 'text';
    columns?: string[];
    rows?: string[][];
    text?: string;
    /** Object URL of the PDF, for the browser's own viewer. */
    pdfUrl?: string;
    /** Saves the file itself. */
    download: () => void;
}

/**
 * One window for everything Go-Live Setup produces.
 *
 * <p>A PDF goes into the browser's own viewer, which already has zoom, find, print and save. A report
 * is drawn as a table, with a find box that keeps only the rows it matches and a text size the reader
 * controls -- a bank file's columns are unreadable shrunk, a hundred rows unreadable large. Saving is
 * two buttons: the file as it was generated, or the table as a PDF through the browser's print.</p>
 */
@Component({
    selector: 'go-live-doc-viewer',
    templateUrl: './doc-viewer.component.html',
    styleUrl: './doc-viewer.component.css',
    imports: [LocalizePipe],
})
export class DocViewerComponent extends AppComponentBase {
    private readonly sanitizer = inject(DomSanitizer);

    readonly doc = input.required<GoLiveDocument>();
    readonly closed = output<void>();

    readonly find = signal('');
    readonly textScale = signal(1);

    readonly safePdfUrl = computed<SafeResourceUrl | null>(() => {
        const url = this.doc().pdfUrl;
        return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
    });

    readonly headings = computed<string[]>(() => this.doc().columns ?? []);

    readonly rows = computed<string[][]>(() => {
        const all = this.doc().rows ?? [];
        const needle = this.find().trim().toLowerCase();
        return needle ? all.filter((row) => row.some((cell) => (cell ?? '').toLowerCase().includes(needle))) : all;
    });

    readonly textLines = computed<string[]>(() => {
        const needle = this.find().trim().toLowerCase();
        const lines = (this.doc().text ?? '').split(/\r?\n/);
        return needle ? lines.filter((line) => line.toLowerCase().includes(needle)) : lines;
    });

    readonly rowCount = computed(() => (this.doc().kind === 'text' ? this.textLines().length : this.rows().length));

    onFind(event: Event): void {
        this.find.set((event.target as HTMLInputElement).value);
    }

    zoom(by: number): void {
        this.textScale.update((scale) => Math.min(2, Math.max(0.6, Math.round((scale + by) * 10) / 10)));
    }

    /** The browser's print dialog, whose "Save as PDF" is how a table becomes a PDF. */
    saveAsPdf(): void {
        window.print();
    }

    close(): void {
        this.closed.emit();
    }
}
