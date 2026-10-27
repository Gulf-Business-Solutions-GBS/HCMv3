import { Component, ElementRef, inject, input, output, signal } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';

import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardSnapshotService } from './dashboard-snapshot.service';

/**
 * The one download icon every dashboard tile (and the dashboard toolbar) carries. Clicking it offers PDF, JPEG and
 * XLSX:
 *
 * - PDF and JPEG are a picture of the tile as it looks on screen, made in the browser (DashboardSnapshotService).
 *   The tile is the nearest `.card` around the icon unless `target` names another element (the toolbar passes the
 *   whole dashboard).
 * - XLSX is the tile's data, which only the server has, so the host component handles it through `(xlsx)`.
 *
 *     <dashboard-export-menu fileName="Leave by type" (xlsx)="exportToExcel(Part.ByType)" />
 */
@Component({
    selector: 'dashboard-export-menu',
    imports: [Menu],
    template: `
        <button
            type="button"
            class="btn btn-sm btn-icon btn-light ld-no-export"
            [disabled]="disabled() || busy()"
            [attr.title]="l('Download')"
            [attr.aria-label]="l('Download')"
            (click)="menu.toggle($event)">
            <i class="fa" [class.fa-download]="!busy()" [class.fa-spinner]="busy()" [class.fa-spin]="busy()"></i>
        </button>
        <p-menu #menu [model]="items" [popup]="true" appendTo="body" />
    `,
})
export class DashboardExportMenuComponent extends AppComponentBase {
    /** Base name of the saved file, without extension. */
    readonly fileName = input.required<string>();

    /** What to capture for PDF/JPEG. Defaults to the tile (nearest `.card`) the icon sits in. */
    readonly target = input<HTMLElement | undefined>(undefined);

    readonly disabled = input(false);

    /** XLSX is built on the server from the tile's data; the host knows which part to ask for. */
    readonly xlsx = output<void>();

    readonly busy = signal(false);

    private readonly _host = inject(ElementRef<HTMLElement>);
    private readonly _snapshot = inject(DashboardSnapshotService);

    readonly items: MenuItem[] = [
        { label: 'PDF', icon: 'fa fa-file-pdf', command: () => this.saveImage('pdf') },
        { label: 'JPEG', icon: 'fa fa-file-image', command: () => this.saveImage('jpeg') },
        { label: 'XLSX', icon: 'fa fa-file-excel', command: () => this.xlsx.emit() },
    ];

    private async saveImage(format: 'pdf' | 'jpeg'): Promise<void> {
        const element = this.target() ?? (this._host.nativeElement.closest('.card') as HTMLElement | null);
        if (!element) {
            return;
        }

        this.busy.set(true);
        try {
            const name = `${this.fileName()} ${new Date().toISOString().slice(0, 10)}`;
            if (format === 'pdf') {
                await this._snapshot.saveAsPdf(element, name);
            } else {
                await this._snapshot.saveAsJpeg(element, name);
            }
        } catch {
            this.notify.error(this.l('Dashboard_DownloadFailed'));
        } finally {
            this.busy.set(false);
        }
    }
}
