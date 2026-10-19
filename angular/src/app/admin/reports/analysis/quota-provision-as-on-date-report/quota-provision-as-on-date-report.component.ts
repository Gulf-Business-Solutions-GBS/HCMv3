import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { CheckboxModule } from 'primeng/checkbox';
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
    GetQuotaProvisionAsOnDateReportInput,
    QuotaProvisionAsOnDateReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface QuotaProvisionAsOnDateReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
    isBool?: boolean;
}

/**
 * Quota Provision As On Date analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptQuotaProvisionAsOnDate.ascx(.cs).
 *
 * AS-ON-DATE report with NO Payroll Area and NO Employee Status filter on the legacy screen (neither
 * combo exists in the legacy markup) -- the shared panel's date range and status filter are switched off
 * and a single As-On date picker plus the required Leave Type and With Projection checkbox are projected
 * into its <ng-content> slot.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './quota-provision-as-on-date-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        DatePicker,
        CheckboxModule,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class QuotaProvisionAsOnDateReportComponent extends AppComponentBase {
    private readonly _reportService = inject(QuotaProvisionAsOnDateReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** Legacy dtFrom, labelled "As On Date". There is no dtTo on this screen. */
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');

    /** Legacy cmbLeaveType selected Code. REQUIRED -- blank yields an empty result. */
    readonly leaveTypeCode = signal<string>('');

    /** Legacy chkWithProjection. */
    readonly withProjection = signal<boolean>(false);

    readonly exporting = signal<boolean>(false);

    private readonly allColumns: QuotaProvisionAsOnDateReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'status', header: this.l('Status'), sortable: false, width: 120 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 170 },
        { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 170 },
        { field: 'leaveTypeName', header: this.l('LeaveType'), sortable: false, width: 160 },
        { field: 'separation', header: this.l('Separation'), sortable: false, width: 120, isBool: true },
        { field: 'fnfPayment', header: this.l('FnfPayment'), sortable: false, width: 130, isBool: true },
        { field: 'asOnDate', header: this.l('AsOnDate'), sortable: false, width: 140, isDate: true },
        { field: 'deductionStart', header: this.l('DeductionStart'), sortable: false, width: 140, isDate: true },
        { field: 'deductionEnd', header: this.l('DeductionEnd'), sortable: false, width: 140, isDate: true },
        { field: 'totalDaysAllowed', header: this.l('TotalDaysAllowed'), sortable: false, width: 160 },
        { field: 'maxLimit', header: this.l('MaxLimit'), sortable: false, width: 130 },
        { field: 'systemOpeningBalanceDays', header: this.l('SystemOpeningBalanceDays'), sortable: false, width: 190 },
        { field: 'accruedQuota', header: this.l('AccruedQuota'), sortable: false, width: 140 },
        { field: 'usedQuota', header: this.l('UsedQuota'), sortable: false, width: 130 },
        { field: 'adjustedQuota', header: this.l('AdjustedQuota'), sortable: false, width: 140 },
        { field: 'forfeitQuota', header: this.l('ForfeitQuota'), sortable: false, width: 140 },
        { field: 'balanceQuota', header: this.l('BalanceQuota'), sortable: false, width: 140 },
        { field: 'balanceProvision', header: this.l('BalanceProvision'), sortable: false, width: 150 },
        { field: 'comment', header: this.l('Comment'), sortable: false, width: 220 },
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
            .getQuotaProvisionAsOnDateReport(
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
            .getQuotaProvisionAsOnDateReportToExcel(
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
    ): GetQuotaProvisionAsOnDateReportInput {
        return new GetQuotaProvisionAsOnDateReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            // Deliberately not sent: legacy has no dtTo on this screen and the backend ignores it.
            toDate: undefined,
            employeeCode: this.employeeCode(),
            // Deliberately not sent: legacy has no Employee Status combo on this screen.
            employeeStatus: undefined,
            leaveTypeCode: this.leaveTypeCode(),
            withProjection: this.withProjection(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
