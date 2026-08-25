import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EmployeeStatus,
    GetLeaveProvisionReportInput,
    LeaveProvisionReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface LeaveProvisionReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Leave Provision analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptLeaveProvision.ascx(.cs)
 * (the ACTIVE-employee path -- see GetLeaveProvisionReportInput remarks for what was not ported).
 *
 * From/To date resolve to month-end boundaries server-side; one row per employee per regular-run month end
 * in between. This screen keeps the shared panel's standard date range and only adds Payroll Area.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './leave-provision-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class LeaveProvisionReportComponent extends AppComponentBase {
    private readonly _reportService = inject(LeaveProvisionReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    /** Legacy cmbPayrollArea selected Code. Blank means "all payroll areas". */
    readonly payrollAreaCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    readonly columns: LeaveProvisionReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'joiningDate', header: this.l('JoiningDate'), sortable: false, width: 140, isDate: true },
        { field: 'leaveDaysCalculationPeriod', header: this.l('LeaveDaysCalculationPeriod'), sortable: true, width: 200 },
        { field: 'payrollAreaName', header: this.l('PayrollArea'), sortable: false, width: 160 },
        { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 170 },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 170 },
        { field: 'basicSalary', header: this.l('BasicSalary'), sortable: false, width: 140 },
        { field: 'allowance', header: this.l('Allowance'), sortable: false, width: 130 },
        { field: 'totalSalary', header: this.l('TotalSalary'), sortable: false, width: 140 },
        { field: 'currencyName', header: this.l('Currency'), sortable: false, width: 120 },
        { field: 'salaryInProvision', header: this.l('SalaryInProvision'), sortable: false, width: 160 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'systemOpeningBalanceDays', header: this.l('SystemOpeningBalanceDays'), sortable: false, width: 190 },
        { field: 'openingBalanceDays', header: this.l('OpeningBalance'), sortable: false, width: 150 },
        { field: 'openingBalanceAmount', header: this.l('OpeningBalanceAmount'), sortable: false, width: 170 },
        { field: 'currentMonthAccrual', header: this.l('CurrentMonthAccrual'), sortable: false, width: 170 },
        { field: 'currentMonthAccrualAmount', header: this.l('CurrentMonthAccrualAmount'), sortable: false, width: 190 },
        { field: 'currentMonthPaidLeave', header: this.l('CurrentMonthPaidLeave'), sortable: false, width: 180 },
        { field: 'currentMonthPaidLeaveAmount', header: this.l('CurrentMonthPaidLeaveAmount'), sortable: false, width: 200 },
        { field: 'currentMonthPaidLeaveArrear', header: this.l('CurrentMonthPaidLeaveArrear'), sortable: false, width: 200 },
        {
            field: 'currentMonthPaidLeaveArrearAmount',
            header: this.l('CurrentMonthPaidLeaveArrearAmount'),
            sortable: false,
            width: 220,
        },
        { field: 'currentMonthAdjustment', header: this.l('CurrentMonthAdjustment'), sortable: false, width: 190 },
        { field: 'currentMonthAdjustmentAmount', header: this.l('CurrentMonthAdjustmentAmount'), sortable: false, width: 210 },
        { field: 'currentMonthForfeit', header: this.l('CurrentMonthForfeit'), sortable: false, width: 180 },
        { field: 'leaveUsage', header: this.l('LeaveUsage'), sortable: false, width: 140 },
        { field: 'currentMonthUnpaidLeave', header: this.l('CurrentMonthUnpaidLeave'), sortable: false, width: 190 },
        { field: 'totalUnpaidLeave', header: this.l('TotalUnpaidLeave'), sortable: false, width: 160 },
        { field: 'absentDays', header: this.l('AbsentDays'), sortable: false, width: 130 },
        { field: 'closingBalance', header: this.l('ClosingBalance'), sortable: false, width: 150 },
        { field: 'closingBalanceAmount', header: this.l('ClosingBalanceAmount'), sortable: false, width: 170 },
    ];

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getLeaveProvisionReport(
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
            .getLeaveProvisionReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetLeaveProvisionReportInput {
        return new GetLeaveProvisionReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            payrollAreaCode: this.payrollAreaCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
