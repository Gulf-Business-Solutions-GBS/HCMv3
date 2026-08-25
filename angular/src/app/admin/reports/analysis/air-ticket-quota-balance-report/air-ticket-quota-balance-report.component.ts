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
    AirTicketQuotaBalanceReportServiceProxy,
    GetAirTicketQuotaBalanceReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface AirTicketQuotaBalanceReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Air Ticket Quota Balance analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptAirTicketQuotaBalance.ascx(.cs).
 *
 * AS-ON-DATE report, no Employee Status combo on the legacy screen (the employee set is hard-coded to
 * Active) -- the shared panel's date range and status filter are switched off and a single As-On date
 * picker plus the Ticket Type filter are projected into its <ng-content> slot.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './air-ticket-quota-balance-report.component.html',
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
export class AirTicketQuotaBalanceReportComponent extends AppComponentBase {
    private readonly _reportService = inject(AirTicketQuotaBalanceReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** Legacy dtFrom, labelled "As On Date". There is no dtTo on this screen. */
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');

    /** Legacy cmbTicketType selected Code. Blank means "all ticket types". */
    readonly ticketTypeCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    readonly columns: AirTicketQuotaBalanceReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'ticketTypeName', header: this.l('TicketTypeCode'), sortable: true, width: 160 },
        { field: 'asOnDate', header: this.l('AsOnDate'), sortable: false, width: 140, isDate: true },
        { field: 'deductionStart', header: this.l('DeductionStart'), sortable: false, width: 140, isDate: true },
        { field: 'deductionEnd', header: this.l('DeductionEnd'), sortable: false, width: 140, isDate: true },
        { field: 'accruedQuota', header: this.l('AccruedQuota'), sortable: false, width: 140 },
        { field: 'usedQuota', header: this.l('UsedQuota'), sortable: false, width: 130 },
        { field: 'adjustedQuota', header: this.l('AdjustedQuota'), sortable: false, width: 140 },
        { field: 'balanceQuota', header: this.l('BalanceQuota'), sortable: false, width: 140 },
        { field: 'comment', header: this.l('Comment'), sortable: false, width: 220 },
    ];

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getAirTicketQuotaBalanceReport(
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
            .getAirTicketQuotaBalanceReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetAirTicketQuotaBalanceReportInput {
        return new GetAirTicketQuotaBalanceReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            // Deliberately not sent: legacy has no dtTo on this screen and the backend ignores it.
            toDate: undefined,
            employeeCode: this.employeeCode(),
            // Deliberately not sent: legacy hard-codes the employee set to Active, no status combo here.
            employeeStatus: undefined,
            ticketTypeCode: this.ticketTypeCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
