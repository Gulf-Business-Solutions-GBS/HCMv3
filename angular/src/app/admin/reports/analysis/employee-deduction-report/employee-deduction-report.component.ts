import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { InputNumber } from 'primeng/inputnumber';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Select } from 'primeng/select';
import { Table, TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    EmployeeDeductionReportServiceProxy,
    GetAllPayrollAreasInput,
    GetEmployeeDeductionReportInput,
    PayrollAreasServiceProxy,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface DeductionReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
}

/**
 * Employee Deduction analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeDeduction.ascx(.cs).
 *
 * ATYPICAL FILTER PANEL: alone in this batch, legacy's panel is Month / Year / Run Type / Payroll Area
 * (uscRptEmployeeDeduction.ascx:15-34) -- no dates, no employee code, no employee status. The shared
 * <grid-report-filter> is still used (so the Get Data / Export buttons and layout stay identical) with all
 * three standard fields switched off, and the four report-specific fields are PROJECTED into its
 * <ng-content> slot rather than forking the component.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the (dead) drill-down stack, the ColumnChooser JSON
 * persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-deduction-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Select,
        InputNumber,
        GridReportFilterComponent,
        LocalizePipe,
    ],
})
export class EmployeeDeductionReportComponent extends AppComponentBase implements OnInit {
    private readonly _reportService = inject(EmployeeDeductionReportServiceProxy);
    private readonly _payrollAreaService = inject(PayrollAreasServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- report-specific filter state (legacy txtMonth / txtYear / cmbRunType / cmbPayrollArea) ---
    readonly month = signal<number | undefined>(new Date().getMonth() + 1);
    readonly year = signal<number | undefined>(new Date().getFullYear());
    readonly payrollRunType = signal<PayrollRunType | undefined>(undefined);
    readonly payrollAreaId = signal<number | undefined>(undefined);

    readonly payrollAreas = signal<{ label: string; value: number }[]>([]);

    readonly runTypeOptions = [
        { label: this.l('Regular'), value: PayrollRunType.Regular },
        { label: this.l('OffCycle'), value: PayrollRunType.OffCycle },
    ];

    readonly exporting = signal<boolean>(false);

    /** Legacy column order (uscRptEmployeeDeduction.ascx.cs:604-623). */
    private readonly allColumns: DeductionReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'jobTitle', header: this.l('JobTitle'), sortable: false, width: 180 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'payrollAreaName', header: this.l('PayrollArea'), sortable: false, width: 160 },
        { field: 'lateEarlyDeduction', header: this.l('LateEarlyDeduction'), sortable: true, width: 200 },
        { field: 'lateEarlyMinutes', header: this.l('LateEarlyMinutes'), sortable: true, width: 200 },
        { field: 'unpaidLeaveDeduction', header: this.l('UnpaidLeaveDeduction'), sortable: true, width: 200 },
        { field: 'unpaidLeaveDays', header: this.l('UnpaidLeaveDays'), sortable: true, width: 180 },
        { field: 'loanRecoveryDeduction', header: this.l('LoanRecoveryDeduction'), sortable: true, width: 200 },
        { field: 'loanRecoveryDays', header: this.l('LoanRecoveryDays'), sortable: true, width: 180 },
        {
            field: 'lateEarlyAfterMonthlyTolerance',
            header: this.l('LateEarlyDeductionAfterMonthlyTolerance'),
            sortable: true,
            width: 280,
        },
        {
            field: 'lateEarlyAfterMonthlyToleranceDays',
            header: this.l('LateEarlyAfterMonthlyToleranceDays'),
            sortable: true,
            width: 280,
        },
    ];

    /** OrgLevel1..7 are captioned with the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    readonly columns = this.orgLevels.columns(this.allColumns);

    /**
     * Only the payroll-area lookup is loaded here. The grid itself is NOT fetched in ngOnInit -- the
     * table's (onLazyLoad) fires the first getRecords().
     */
    ngOnInit(): void {
        this._payrollAreaService
            .getAll(
                new GetAllPayrollAreasInput({
                    filter: undefined,
                    sorting: 'Code ASC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .subscribe((result) => {
                this.payrollAreas.set(
                    (result.items || []).map((area) => ({
                        label: area.code + ' - ' + area.name,
                        value: area.id,
                    }))
                );
            });
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getEmployeeDeductionReport(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getEmployeeDeductionReportToExcel(
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

    private buildInput(
        sorting: string,
        maxResultCount: number,
        skipCount: number
    ): GetEmployeeDeductionReportInput {
        return new GetEmployeeDeductionReportInput({
            // The four inherited base filters have no UI on this screen -- legacy has none either.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            month: this.month(),
            year: this.year(),
            payrollRunType: this.payrollRunType(),
            payrollAreaId: this.payrollAreaId(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
