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
    GetIndemnityProvisionAsOnDateReportInput,
    IndemnityProvisionAsOnDateReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface IndemnityProvisionAsOnDateReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
    isBool?: boolean;
}

/**
 * Indemnity Provision As On Date analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptIndemnityProvisionAsOnDate.ascx(.cs).
 *
 * AS-ON-DATE report: legacy has no dtTo, only an "As On Date" picker -- the shared panel's date range is
 * switched off and a single As-On date picker is projected into its <ng-content> slot.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './indemnity-provision-as-on-date-report.component.html',
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
export class IndemnityProvisionAsOnDateReportComponent extends AppComponentBase {
    private readonly _reportService = inject(IndemnityProvisionAsOnDateReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** Legacy dtFrom, labelled "As On Date". There is no dtTo on this screen. */
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    private readonly allColumns: IndemnityProvisionAsOnDateReportColumn[] = [
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
        { field: 'costCenterGroupName', header: this.l('CostCenterGroup'), sortable: false, width: 180 },
        { field: 'separation', header: this.l('Separation'), sortable: false, width: 120, isBool: true },
        { field: 'fnfPayment', header: this.l('FnfPayment'), sortable: false, width: 130, isBool: true },
        { field: 'asOnDate', header: this.l('AsOnDate'), sortable: false, width: 140, isDate: true },
        { field: 'deductionStart', header: this.l('DeductionStart'), sortable: false, width: 140, isDate: true },
        { field: 'deductionEnd', header: this.l('DeductionEnd'), sortable: false, width: 140, isDate: true },
        { field: 'indemnityRate', header: this.l('IndemnityRate'), sortable: false, width: 140 },
        { field: 'indemnityQuota', header: this.l('IndemnityQuota'), sortable: false, width: 150 },
        { field: 'indemnityAmount', header: this.l('IndemnityAmount'), sortable: false, width: 160 },
        { field: 'paidAmount', header: this.l('PaidAmount'), sortable: false, width: 140 },
        { field: 'balanceAmount', header: this.l('BalanceAmount'), sortable: false, width: 140 },
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
            .getIndemnityProvisionAsOnDateReport(
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
            .getIndemnityProvisionAsOnDateReportToExcel(
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
    ): GetIndemnityProvisionAsOnDateReportInput {
        return new GetIndemnityProvisionAsOnDateReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            // Deliberately not sent: legacy has no dtTo on this screen and the backend ignores it.
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
