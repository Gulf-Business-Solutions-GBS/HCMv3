import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
    GetLeaveQuotaBalanceReportInput,
    LeaveQuotaBalanceReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface LeaveQuotaBalanceReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Leave Quota Balance analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptLeaveQuotaBalance.ascx(.cs).
 *
 * NO DATE FILTER: legacy has no From/To date field on this screen (the As-On-Date is fixed to "today") --
 * the shared panel's date range is switched off. Leave Type is REQUIRED by the backend: with none supplied
 * the server returns an empty result, matching legacy's own behaviour.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './leave-quota-balance-report.component.html',
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
export class LeaveQuotaBalanceReportComponent extends AppComponentBase {
    private readonly _reportService = inject(LeaveQuotaBalanceReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    /** Legacy cmbLeaveType selected Code. REQUIRED -- blank yields an empty result. */
    readonly leaveTypeCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    private readonly allColumns: LeaveQuotaBalanceReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 170 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'leaveTypeName', header: this.l('LeaveType'), sortable: false, width: 160 },
        { field: 'deductionStart', header: this.l('DeductionStart'), sortable: false, width: 140, isDate: true },
        { field: 'deductionEnd', header: this.l('DeductionEnd'), sortable: false, width: 140, isDate: true },
        { field: 'totalDaysAllowed', header: this.l('TotalDaysAllowed'), sortable: false, width: 160 },
        { field: 'accruedQuota', header: this.l('AccruedQuota'), sortable: false, width: 140 },
        { field: 'usedQuota', header: this.l('UsedQuota'), sortable: false, width: 130 },
        { field: 'adjustedQuota', header: this.l('AdjustedQuota'), sortable: false, width: 140 },
        { field: 'forfeitedQuota', header: this.l('ForfeitedQuota'), sortable: false, width: 150 },
        { field: 'balanceQuota', header: this.l('BalanceQuota'), sortable: false, width: 140 },
        { field: 'comment', header: this.l('Comment'), sortable: false, width: 220 },
        { field: 'workLocationName', header: this.l('WorkLocation'), sortable: false, width: 160 },
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
            .getLeaveQuotaBalanceReport(
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
            .getLeaveQuotaBalanceReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetLeaveQuotaBalanceReportInput {
        return new GetLeaveQuotaBalanceReportInput({
            // Deliberately not sent: legacy has no date field on this screen, the As-On-Date is fixed to
            // "today" and the backend ignores these.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            leaveTypeCode: this.leaveTypeCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
