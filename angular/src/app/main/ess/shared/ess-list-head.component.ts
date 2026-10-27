import { ChangeDetectionStrategy, Component, ViewEncapsulation, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    EssListExportColumnDto,
    EssListExportColumnType,
    EssListExportInput,
    EssListExportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { AppConsts } from '@shared/AppConsts';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LocalizationService } from 'abp-ng2-module';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { EssList, EssListColumn } from './ess-list';

/**
 * The heading of every self-service request list: the column titles, which sort when clicked, and under them
 * a row of filters. Sits on the table's own <c>thead</c> so each screen keeps its plain table and its own
 * cells.
 *
 * <p>The last, untitled column is the one the row buttons sit in; it holds Export to Excel and, on
 * the filter row, Clear filters.</p>
 */
@Component({
    // eslint-disable-next-line @angular-eslint/component-selector
    selector: 'thead[essListHead]',
    host: { class: 'ess-list-head' },
    imports: [CommonModule, FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.OnPush,

    // Unscoped on purpose: the side padding has to reach the body cells of the screen's own table as well,
    // or the first and last columns sit hard against the edge of the shaded heading.
    encapsulation: ViewEncapsulation.None,
    styles: [
        `
            thead.ess-list-head th.ess-list-sortable {
                cursor: pointer;
                user-select: none;
                white-space: nowrap;
            }

            thead.ess-list-head th.ess-list-sortable .fa {
                opacity: 0.35;
            }

            thead.ess-list-head th.ess-list-sortable .fa.ess-list-sorted {
                opacity: 1;
            }

            thead.ess-list-head tr.ess-list-filters th {
                padding-top: 0;
                font-weight: normal;
            }

            thead.ess-list-head tr.ess-list-filters .form-control,
            thead.ess-list-head tr.ess-list-filters .form-select {
                min-width: 5rem;
            }

            /* ".table" twice over: the theme zeroes the first and last cell's padding with a rule this has to outrank. */
            table.table.table:has(> thead.ess-list-head) > * > tr > :first-child {
                padding-inline-start: 1rem;
            }

            table.table.table:has(> thead.ess-list-head) > * > tr > :last-child {
                padding-inline-end: 1rem;
            }
        `,
    ],
    template: `
        <tr>
            @for (column of list().columns; track column.key) {
                @if (column.label) {
                    <th
                        class="ess-list-sortable"
                        [class.text-end]="column.align === 'end'"
                        [attr.data-help-loc]="column.label"
                        (click)="list().toggleSort(column.key)">
                        {{ column.label | localize }}
                        <i class="fa ms-1" [ngClass]="sortIcon(column.key)"></i>
                    </th>
                } @else {
                    <!-- An untitled column (an expand arrow, say): nothing to sort by. -->
                    <th></th>
                }
            }
            <th class="text-end">
                <button
                    type="button"
                    class="btn btn-light btn-sm text-nowrap"
                    data-help-loc="ExportToExcel"
                    [disabled]="exporting()"
                    (click)="export()">
                    <i class="fa fa-file-excel"></i>
                    {{ 'ExportToExcel' | localize }}
                </button>
            </th>
        </tr>
        <tr class="ess-list-filters" data-help-loc="FilterRow">
            @for (column of list().columns; track column.key) {
                <th>
                    @if (column.filter === 'select') {
                        <select
                            class="form-select form-select-sm"
                            [attr.aria-label]="column.label | localize"
                            [ngModel]="list().filters()[column.key] ?? ''"
                            (ngModelChange)="list().setFilter(column.key, $event)">
                            <option value="">{{ 'All' | localize }}</option>
                            @for (option of list().optionsOf(column); track option) {
                                <option [value]="option">{{ option }}</option>
                            }
                        </select>
                    } @else if (column.filter !== 'none') {
                        <input
                            type="text"
                            class="form-control form-control-sm"
                            [attr.aria-label]="column.label | localize"
                            [ngModel]="list().filters()[column.key] ?? ''"
                            (ngModelChange)="list().setFilter(column.key, $event)" />
                    }
                </th>
            }
            <th class="text-end">
                <button type="button" class="btn btn-light btn-sm text-nowrap" (click)="list().clearFilters()">
                    {{ 'ClearFilters' | localize }}
                </button>
            </th>
        </tr>
    `,
})
export class EssListHeadComponent {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    readonly list = input.required<EssList<any>>({ alias: 'essListHead' });

    /** The screen's title: it names the exported sheet and file. */
    readonly exportTitle = input('');

    readonly exporting = signal(false);

    private readonly _exportService = inject(EssListExportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);
    private readonly _localization = inject(LocalizationService);

    sortIcon(key: string): string {
        if (this.list().sortKey() !== key) {
            return 'fa-sort';
        }

        return this.list().sortDescending() ? 'fa-sort-down ess-list-sorted' : 'fa-sort-up ess-list-sorted';
    }
    /**
     * Export to Excel: every row the filters let through, in the order shown -- not only the page on screen.
     * A screen whose rows are more than they show (leave) supplies its own export; the rest send the list as
     * it stands and get a workbook back.
     */
    export(): void {
        const list = this.list();

        if (list.exporter) {
            list.exporter();
            return;
        }

        const columns = list.columns.filter((column) => !!column.label);
        const rows = list.filtered();
        const types = columns.map((column) => this.typeOf(column, rows));

        const input = new EssListExportInput();
        input.title = this.exportTitle() || document.title;
        input.columns = columns.map((column, index) => {
            const dto = new EssListExportColumnDto();
            dto.header = this._localization.localize(
                column.label,
                AppConsts.localization.defaultLocalizationSourceName
            );
            dto.type = types[index];

            return dto;
        });
        input.rows = rows.map((row) => columns.map((column, index) => this.cell(column, row, types[index])));

        this.exporting.set(true);

        this._exportService
            .exportToExcel(input)
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    /**
     * What a column holds, judged from its first filled cell: a date when it shows one and sorts by a point
     * in time, a number when it shows digits, text otherwise. Excel then sorts and totals it properly.
     */
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    private typeOf(column: EssListColumn<any>, rows: any[]): EssListExportColumnType {
        const sample = rows.find((row) => {
            const shown = column.text(row);
            return shown !== undefined && shown !== null && shown !== '';
        });

        if (sample === undefined) {
            return EssListExportColumnType.Text;
        }

        const shown = column.text(sample);
        const sortsBy = column.sort?.(sample);

        if (typeof sortsBy === 'number' && typeof shown === 'string') {
            if (/\d{4}-\d{2}-\d{2}|\d{2}\/\d{2}\/\d{4}/.test(shown)) {
                return EssListExportColumnType.Date;
            }

            // "1,234.500" sorted by 1234.5 is an amount; "09:30" sorted by minutes is not.
            return /^-?[\d,]+(\.\d+)?$/.test(shown) ? EssListExportColumnType.Number : EssListExportColumnType.Text;
        }

        return typeof shown === 'number' ? EssListExportColumnType.Number : EssListExportColumnType.Text;
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    private cell(column: EssListColumn<any>, row: any, type: EssListExportColumnType): string {
        const shown = column.text(row);
        const sortsBy = column.sort?.(row);

        if (type === EssListExportColumnType.Date && typeof sortsBy === 'number') {
            return DateTime.fromMillis(sortsBy).toISODate() ?? '';
        }

        if (type === EssListExportColumnType.Number) {
            const value = typeof sortsBy === 'number' ? sortsBy : shown;
            return typeof value === 'number' ? `${value}` : `${shown ?? ''}`;
        }

        return `${shown ?? ''}`;
    }
}
