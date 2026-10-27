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
    GetPayrollJVCustom017ReportInput,
    PayrollJVCustom017Format,
    PayrollJVCustom017ReportServiceProxy,
    PayrollJVReportColumnDto,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Payroll JV (Custom 017) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom017.ascx(.cs).
 *
 * SHAPE: this variant emits a MICROSOFT DYNAMICS AX JOURNAL IMPORT FILE -- one row per salary-register
 * line, plus an offset row per provision and one closing Net Pay row -- with AX's own 27 column names.
 * The column list still comes from the server so the grid and the exported xlsx cannot drift apart.
 *
 * The Type combo is kept because legacy shows it, but it selects NOTHING: legacy's live Table1() never
 * reads it, and only the execution-log parameter string and the export file name use it. See the app
 * service remarks; the behaviour is pinned by a test.
 *
 * ATYPICAL FILTER PANEL vs the reference screen (employee-contract-report): legacy's panel is
 * Month/Year, Run Type, Payroll Area, Type and Employee Code. There is no date range and no
 * employee-status combo, so both are switched off on <grid-report-filter> rather than by forking it;
 * the employee-code box IS used and is left on.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './payroll-jv-custom017-report.component.html',
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
export class PayrollJVCustom017ReportComponent extends AppComponentBase {
    private readonly _reportService = inject(PayrollJVCustom017ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- report-specific filters (legacy txtMonth/txtYear/cmbRunType/cmbPayrollArea/cmbType) ---
    readonly month = signal<number | undefined>(new Date().getMonth() + 1);
    readonly year = signal<number | undefined>(new Date().getFullYear());
    readonly payrollRunType = signal<PayrollRunType | undefined>(undefined);
    readonly payrollAreaId = signal<number | undefined>(undefined);
    readonly format = signal<PayrollJVCustom017Format>(PayrollJVCustom017Format.MainFormat);

    /** Legacy employee-code box; comma-separated codes. */
    readonly employeeCode = signal<string>('');

    readonly runTypeOptions = [
        { label: this.l('Regular'), value: PayrollRunType.Regular },
        { label: this.l('OffCycle'), value: PayrollRunType.OffCycle },
    ];

    /** Legacy cmbType. Offered because legacy offers it -- it changes no data. */
    readonly formatOptions = [
        { label: this.l('MainFormat'), value: PayrollJVCustom017Format.MainFormat },
        { label: this.l('ProvisionFormat'), value: PayrollJVCustom017Format.ProvisionFormat },
        { label: this.l('MissingAccounts'), value: PayrollJVCustom017Format.MissingAccounts },
        { label: this.l('EmployeeWiseBreakDown'), value: PayrollJVCustom017Format.EmployeeWiseBreakDown },
    ];

    readonly exporting = signal<boolean>(false);

    /** Column list as returned by the server; empty until the first load. */
    readonly columns = signal<PayrollJVReportColumnDto[]>([]);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getPayrollJVCustom017Report(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.columns.set(result.columns || []);
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items || []);
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
            .getPayrollJVCustom017ReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetPayrollJVCustom017ReportInput {
        return new GetPayrollJVCustom017ReportInput({
            // Present-but-undefined on purpose: NSwag types these as `T | undefined`, not optional, so
            // they must still be listed even though this screen has no date range and no status combo.
            fromDate: undefined,
            toDate: undefined,
            employeeStatus: undefined,
            employeeCode: this.employeeCode(),
            month: this.month(),
            year: this.year(),
            payrollRunType: this.payrollRunType(),
            payrollAreaId: this.payrollAreaId(),
            format: this.format(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
