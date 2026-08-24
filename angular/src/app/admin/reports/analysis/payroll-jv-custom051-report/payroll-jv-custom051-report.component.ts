import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    GetPayrollJVCustom051ReportInput,
    PayrollJVCustom051ReportColumnDto,
    PayrollJVCustom051ReportRowDto,
    PayrollJVCustom051ReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Payroll JV (Custom 051) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom051.ascx(.cs).
 *
 * SERVER-DRIVEN COLUMNS. The 28 columns are an ERP journal-voucher import layout whose headers are
 * literal tokens ("Amount|Debit", "#headerconnector", "Subsidiary: Entity Type"). They come back with the
 * rows so the grid and the exported xlsx are driven by ONE list, and they are rendered UNTRANSLATED --
 * passing them through the localize pipe would render "[Amount|Debit]" and break the finance import.
 *
 * ATYPICAL FILTER PANEL: legacy's panel is Month / Year / Payroll Area only. There is no date range, no
 * employee-code box and no employee-status combo, so all three are switched off on <grid-report-filter>
 * rather than by forking it.
 *
 * NOTE FOR USERS: the legacy Payroll Area filter never actually narrowed the data (the control filtered a
 * query it then discarded) and the port reproduces that. See PayrollJVCustom051ReportAppService.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './payroll-jv-custom051-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Tooltip,
        GridReportFilterComponent,
        LocalizePipe,
    ],
})
export class PayrollJVCustom051ReportComponent extends AppComponentBase {
    private readonly _reportService = inject(PayrollJVCustom051ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- report-specific filters (legacy txtMonth/txtYear/cmbPayrollArea) ---
    readonly month = signal<number | undefined>(new Date().getMonth() + 1);
    readonly year = signal<number | undefined>(new Date().getFullYear());
    readonly payrollAreaId = signal<number | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    readonly columns = signal<PayrollJVCustom051ReportColumnDto[]>([]);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getPayrollJVCustom051Report(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                const columns = result.columns || [];
                this.columns.set(columns);

                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set((result.items || []).map((item) => this.flatten(item, columns)));
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    /** Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page. */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getPayrollJVCustom051ReportToExcel(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage,
                    0
                )
            )
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
    }

    /** Flattens the row's two dictionaries onto the column names the server declared. */
    private flatten(row: PayrollJVCustom051ReportRowDto, columns: PayrollJVCustom051ReportColumnDto[]): Record<string, unknown> {
        const flat: Record<string, unknown> = { oid: row.id, sortOrder: row.sortOrder };

        for (const column of columns) {
            flat[column.name] = column.isNumeric ? (row.amounts?.[column.name] ?? 0) : (row.values?.[column.name] ?? '');
        }

        return flat;
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetPayrollJVCustom051ReportInput {
        return new GetPayrollJVCustom051ReportInput({
            // Present-but-undefined on purpose: NSwag types these as `T | undefined`, not optional, so they
            // must still be listed even though this screen has no date range, code box or status combo.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            month: this.month(),
            year: this.year(),
            payrollAreaId: this.payrollAreaId(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
