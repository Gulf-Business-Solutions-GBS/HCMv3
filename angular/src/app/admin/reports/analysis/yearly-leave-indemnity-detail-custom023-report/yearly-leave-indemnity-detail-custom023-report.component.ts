import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { DatePicker } from 'primeng/datepicker';
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
    GetYearlyLeaveIndemnityDetailReportInput,
    YearlyLeaveIndemnityDetailCustom023ReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface YearlyLeaveIndemnityDetailReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Yearly Leave Indemnity Detail (Custom023) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptYearlyLeaveIndemnityDetail_Custom023.ascx(.cs).
 * Business logic is byte-for-byte identical to the Custom016 screen -- see
 * GetYearlyLeaveIndemnityDetailReportInput remarks -- ported as a separate app service/screen only because
 * the legacy screens, permissions and execution-log entries are separate.
 *
 * AS-ON-DATE (year-end) report: the legacy "As On Date" is the To Date, there is no From Date filter on
 * this screen -- the shared panel's date range is switched off and a single As-On date picker (bound to
 * toDate) plus Payroll Area are projected into its <ng-content> slot.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './yearly-leave-indemnity-detail-custom023-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        DatePicker,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class YearlyLeaveIndemnityDetailCustom023ReportComponent extends AppComponentBase {
    private readonly _reportService = inject(YearlyLeaveIndemnityDetailCustom023ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** Legacy txtYear/EndDate, labelled "As On Date". There is no From Date on this screen. */
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    /** Legacy payrollArea Session key. Blank means "all payroll areas". */
    readonly payrollAreaCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    private readonly allColumns: YearlyLeaveIndemnityDetailReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'status', header: this.l('Status'), sortable: false, width: 120 },
        { field: 'joiningDate', header: this.l('JoiningDate'), sortable: false, width: 140, isDate: true },
        { field: 'lastDayInService', header: this.l('LastDayInService'), sortable: false, width: 150, isDate: true },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 170 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'costCenterCode', header: this.l('CostCenterCode'), sortable: false, width: 150 },
        { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 170 },
        { field: 'costCenterGroupName', header: this.l('CostCenterGroup'), sortable: false, width: 180 },
        { field: 'totalDaysAllowed', header: this.l('TotalDaysAllowed'), sortable: false, width: 160 },
        { field: 'openingBalance', header: this.l('OpeningBalance'), sortable: false, width: 150 },
        { field: 'accruedQuota', header: this.l('AccruedQuota'), sortable: false, width: 140 },
        { field: 'usedQuota', header: this.l('UsedQuota'), sortable: false, width: 130 },
        { field: 'adjustedQuota', header: this.l('AdjustedQuota'), sortable: false, width: 140 },
        { field: 'forfeitQuota', header: this.l('ForfeitQuota'), sortable: false, width: 140 },
        { field: 'salaryInProvision', header: this.l('SalaryInProvision'), sortable: false, width: 160 },
        { field: 'closingBalance', header: this.l('ClosingBalance'), sortable: false, width: 150 },
        { field: 'closingProvision', header: this.l('ClosingProvision'), sortable: false, width: 150 },
        { field: 'indemnityClosingBalance', header: this.l('IndemnityClosingBalance'), sortable: false, width: 190 },
        {
            field: 'indemnityClosingBalanceAmount',
            header: this.l('IndemnityClosingBalanceAmount'),
            sortable: false,
            width: 210,
        },
    ];

    /** OrgLevel1..7 are captioned with the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    readonly columns = this.orgLevels.columns(this.allColumns);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getYearlyLeaveIndemnityDetailCustom023Report(
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
            .getYearlyLeaveIndemnityDetailCustom023ReportToExcel(
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
    ): GetYearlyLeaveIndemnityDetailReportInput {
        return new GetYearlyLeaveIndemnityDetailReportInput({
            // Deliberately not sent: legacy has no From Date filter on this screen.
            fromDate: undefined,
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
