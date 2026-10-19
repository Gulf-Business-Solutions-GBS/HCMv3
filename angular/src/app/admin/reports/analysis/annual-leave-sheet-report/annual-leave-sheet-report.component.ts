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
    AnnualLeaveSheetReportServiceProxy,
    GetAnnualLeaveSheetReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface AnnualLeaveSheetReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Annual Leave Sheet analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptAnnualLeaveSheet.ascx(.cs).
 *
 * AS-ON-DATE report, no Employee Status combo on the legacy screen (employee set hard-coded to Active) --
 * the shared panel's date range and status filter are switched off and a single As-On date picker plus the
 * Payroll Area filter are projected into its <ng-content> slot.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './annual-leave-sheet-report.component.html',
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
export class AnnualLeaveSheetReportComponent extends AppComponentBase {
    private readonly _reportService = inject(AnnualLeaveSheetReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** Legacy dtFrom, labelled "As On Date". There is no dtTo on this screen. */
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');

    /** Legacy cmbPayrollArea selected Code. Blank means "all payroll areas". */
    readonly payrollAreaCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    private readonly allColumns: AnnualLeaveSheetReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 180 },
        { field: 'costCenterName', header: this.l('CostCenter'), sortable: false, width: 180 },
        { field: 'asOnDate', header: this.l('AsOnDate'), sortable: false, width: 140, isDate: true },
        { field: 'perYearQuota', header: this.l('PerYearQuota'), sortable: false, width: 140 },
        { field: 'maxLimit', header: this.l('MaxLimit'), sortable: false, width: 130 },
        { field: 'openingBalance', header: this.l('OpeningBalance'), sortable: false, width: 150 },
        { field: 'quotaAccrued', header: this.l('QuotaAccrued'), sortable: false, width: 140 },
        { field: 'quotaUsed', header: this.l('QuotaUsed'), sortable: false, width: 130 },
        { field: 'quotaAdjusted', header: this.l('QuotaAdjusted'), sortable: false, width: 140 },
        { field: 'quotaForfeited', header: this.l('QuotaForfeited'), sortable: false, width: 150 },
        { field: 'quotaBalance', header: this.l('QuotaBalance'), sortable: false, width: 140 },
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
            .getAnnualLeaveSheetReport(
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
            .getAnnualLeaveSheetReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetAnnualLeaveSheetReportInput {
        return new GetAnnualLeaveSheetReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            // Deliberately not sent: legacy has no dtTo on this screen and the backend ignores it.
            toDate: undefined,
            employeeCode: this.employeeCode(),
            // Deliberately not sent: legacy hard-codes the employee set to Active, no status combo here.
            employeeStatus: undefined,
            payrollAreaCode: this.payrollAreaCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
