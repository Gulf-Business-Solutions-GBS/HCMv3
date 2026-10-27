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
    GetPayrollJVCustom030ReportInput,
    PayrollJVCustom030ReportColumnDto,
    PayrollJVCustom030ReportRowDto,
    PayrollJVCustom030ReportServiceProxy,
    PayrollJVCustom030ReportType,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Payroll JV (Custom 030) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom030.ascx(.cs).
 *
 * FIVE SHEETS BEHIND ONE SCREEN. The legacy cmbType combo selects Salary JV (8 columns), Missing Accounts
 * (10), EmployeeWise BreakDown (7), Indemnity JV (5) or Annual Leave JV (5) -- completely different column
 * sets, so the column list is served by the backend alongside the rows and the grid rebuilds its header
 * from it.
 *
 * This customer's voucher is a two-column PIVOT ("Ops" and "Admin", the two CostElement1 codes) rather
 * than one line per account, and the two balance sheets are a four/five line movement.
 *
 * NOTE FOR USERS: the Indemnity JV and Annual Leave JV sheets ignore Payroll Area, Run Type and Employee
 * Code -- legacy re-queries from scratch for them and the port reproduces that.
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
    templateUrl: './payroll-jv-custom030-report.component.html',
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
export class PayrollJVCustom030ReportComponent extends AppComponentBase {
    private readonly _reportService = inject(PayrollJVCustom030ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- report-specific filters (legacy txtMonth/txtYear/cmbPayrollArea/cmbRunType/cmbType) ---
    readonly month = signal<number | undefined>(new Date().getMonth() + 1);
    readonly year = signal<number | undefined>(new Date().getFullYear());
    readonly payrollAreaId = signal<number | undefined>(undefined);
    readonly payrollRunType = signal<PayrollRunType | undefined>(undefined);
    readonly reportType = signal<PayrollJVCustom030ReportType>(PayrollJVCustom030ReportType.SalaryJV);
    readonly employeeCode = signal<string>('');

    readonly runTypeOptions = [
        { label: this.l('Regular'), value: PayrollRunType.Regular },
        { label: this.l('OffCycle'), value: PayrollRunType.OffCycle },
    ];

    readonly reportTypeOptions = [
        { label: this.l('PayrollJVCustom030TypeSalaryJV'), value: PayrollJVCustom030ReportType.SalaryJV },
        { label: this.l('PayrollJVCustom030TypeMissingAccounts'), value: PayrollJVCustom030ReportType.MissingAccounts },
        {
            label: this.l('PayrollJVCustom030TypeEmployeeWiseBreakDown'),
            value: PayrollJVCustom030ReportType.EmployeeWiseBreakDown,
        },
        { label: this.l('PayrollJVCustom030TypeIndemnityJV'), value: PayrollJVCustom030ReportType.IndemnityJV },
        { label: this.l('PayrollJVCustom030TypeAnnualLeaveJV'), value: PayrollJVCustom030ReportType.AnnualLeaveJV },
    ];

    readonly exporting = signal<boolean>(false);

    readonly columns = signal<PayrollJVCustom030ReportColumnDto[]>([]);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getPayrollJVCustom030Report(
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
            .getPayrollJVCustom030ReportToExcel(
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
    private flatten(
        row: PayrollJVCustom030ReportRowDto,
        columns: PayrollJVCustom030ReportColumnDto[]
    ): Record<string, unknown> {
        const flat: Record<string, unknown> = { oid: row.serialNumber };

        for (const column of columns) {
            flat[column.name] = column.isNumeric
                ? (row.amounts?.[column.name] ?? 0)
                : (row.values?.[column.name] ?? '');
        }

        return flat;
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetPayrollJVCustom030ReportInput {
        return new GetPayrollJVCustom030ReportInput({
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
