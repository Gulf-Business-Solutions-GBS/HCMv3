import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Select } from 'primeng/select';
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
    GetPayrollJVCustom035ReportInput,
    PayrollJVCustom035ReportColumnDto,
    PayrollJVCustom035ReportRowDto,
    PayrollJVCustom035ReportServiceProxy,
    PayrollJVCustom035ReportType,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Payroll JV (Custom 035) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom035.ascx(.cs).
 *
 * THREE SHEETS BEHIND ONE SCREEN. The legacy cmbType combo selects Main Format (13 columns), Missing
 * Accounts (14) or EmployeeWise BreakDown (15) -- completely different column sets, so the column list is
 * served by the backend alongside the rows and the grid rebuilds its header from it.
 *
 * Headers are rendered UNTRANSLATED: they are ERP import tokens ("lines.debit.value.amount") and legacy
 * captions the customer reconciles against, not localization keys.
 *
 * ATYPICAL FILTER PANEL: Payroll Area / Run Type / Month / Year / Type / Employee Code. No date range and
 * no employee status, so those two are switched off on <grid-report-filter> rather than by forking it.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './payroll-jv-custom035-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Select,
        Tooltip,
        GridReportFilterComponent,
        LocalizePipe,
    ],
})
export class PayrollJVCustom035ReportComponent extends AppComponentBase {
    private readonly _reportService = inject(PayrollJVCustom035ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- report-specific filters (legacy txtMonth/txtYear/cmbPayrollArea/cmbRunType/cmbType) ---
    readonly month = signal<number | undefined>(new Date().getMonth() + 1);
    readonly year = signal<number | undefined>(new Date().getFullYear());
    readonly payrollAreaId = signal<number | undefined>(undefined);
    readonly payrollRunType = signal<PayrollRunType | undefined>(undefined);
    readonly reportType = signal<PayrollJVCustom035ReportType>(PayrollJVCustom035ReportType.MainFormat);
    readonly employeeCode = signal<string>('');

    readonly runTypeOptions = [
        { label: this.l('Regular'), value: PayrollRunType.Regular },
        { label: this.l('OffCycle'), value: PayrollRunType.OffCycle },
    ];

    readonly reportTypeOptions = [
        { label: this.l('PayrollJVCustom035TypeMainFormat'), value: PayrollJVCustom035ReportType.MainFormat },
        { label: this.l('PayrollJVCustom035TypeMissingAccounts'), value: PayrollJVCustom035ReportType.MissingAccounts },
        {
            label: this.l('PayrollJVCustom035TypeEmployeeWiseBreakDown'),
            value: PayrollJVCustom035ReportType.EmployeeWiseBreakDown,
        },
    ];

    readonly exporting = signal<boolean>(false);

    readonly columns = signal<PayrollJVCustom035ReportColumnDto[]>([]);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getPayrollJVCustom035Report(
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
            .getPayrollJVCustom035ReportToExcel(
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
    private flatten(row: PayrollJVCustom035ReportRowDto, columns: PayrollJVCustom035ReportColumnDto[]): Record<string, unknown> {
        const flat: Record<string, unknown> = { oid: row.serialNumber };

        for (const column of columns) {
            flat[column.name] = column.isNumeric ? (row.amounts?.[column.name] ?? 0) : (row.values?.[column.name] ?? '');
        }

        return flat;
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetPayrollJVCustom035ReportInput {
        return new GetPayrollJVCustom035ReportInput({
            // Present-but-undefined on purpose: NSwag types these as `T | undefined`, not optional, so they
            // must still be listed even though this screen has no date range and no status combo.
            fromDate: undefined,
            toDate: undefined,
            employeeStatus: undefined,
            employeeCode: this.employeeCode(),
            month: this.month(),
            year: this.year(),
            payrollAreaId: this.payrollAreaId(),
            payrollRunType: this.payrollRunType(),
            reportType: this.reportType(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
