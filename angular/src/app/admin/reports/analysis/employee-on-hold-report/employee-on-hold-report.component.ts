import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
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
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EmployeeOnHoldReportServiceProxy,
    GetEmployeeOnHoldReportInput,
    OnHoldStatus,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface OnHoldReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
    isBool?: boolean;
}

/**
 * Employee On-Hold analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Status/uscRptEmployeeOnHold.ascx(.cs).
 *
 * DEVIATION: legacy's "Employee Status" box is actually bound to the OnHoldStatus/BankTransfer field --
 * see GetEmployeeOnHoldReportInput remarks. The shared panel's employeeStatus filter is hidden and this
 * correctly-typed BankTransfer filter is projected in instead.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-on-hold-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Select,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class EmployeeOnHoldReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EmployeeOnHoldReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');

    /** Legacy cmbEmpStatus (mislabelled -- actually Bank Transfer / OnHold-type filter). */
    readonly bankTransfer = signal<OnHoldStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    readonly bankTransferOptions = [
        { label: this.l('NA'), value: OnHoldStatus.NA },
        { label: this.l('Hold'), value: OnHoldStatus.Hold },
        { label: this.l('Unhold'), value: OnHoldStatus.Unhold },
    ];

    private readonly allColumns: OnHoldReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'jobTitleName', header: this.l('JobTitle'), sortable: false, width: 180 },
        { field: 'civilId', header: this.l('CivilId'), sortable: false, width: 150 },
        { field: 'startDate', header: this.l('StartDate'), sortable: false, width: 140, isDate: true },
        { field: 'endDate', header: this.l('EndDate'), sortable: false, width: 140, isDate: true },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'onHold', header: this.l('OnHold'), sortable: false, width: 110, isBool: true },
        { field: 'onHoldBasicPay', header: this.l('OnHoldBasicPay'), sortable: false, width: 150, isBool: true },
        { field: 'onHoldLeavePay', header: this.l('OnHoldLeavePay'), sortable: false, width: 150, isBool: true },
        {
            field: 'onHoldIndemnityPay',
            header: this.l('OnHoldIndemnityPay'),
            sortable: false,
            width: 160,
            isBool: true,
        },
        {
            field: 'onHoldAirTicketPay',
            header: this.l('OnHoldAirTicketPay'),
            sortable: false,
            width: 160,
            isBool: true,
        },
        {
            field: 'onHoldLoanDeduction',
            header: this.l('OnHoldLoanDeduction'),
            sortable: false,
            width: 160,
            isBool: true,
        },
        {
            field: 'onHoldAllEarnings',
            header: this.l('OnHoldAllEarnings'),
            sortable: false,
            width: 160,
            isBool: true,
        },
        {
            field: 'onHoldAllDeductions',
            header: this.l('OnHoldAllDeductions'),
            sortable: false,
            width: 170,
            isBool: true,
        },
        { field: 'bankTransfer', header: this.l('BankTransfer'), sortable: false, width: 150 },
        { field: 'comments', header: this.l('Comments'), sortable: false, width: 220 },
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
            .getEmployeeOnHoldReport(
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
            .getEmployeeOnHoldReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeOnHoldReportInput {
        return new GetEmployeeOnHoldReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            bankTransfer: this.bankTransfer(),
            employeeStatus: undefined,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
