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
    EndOfServiceReportServiceProxy,
    GetEndOfServiceReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface EndOfServiceReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
    isBool?: boolean;
}

/**
 * End Of Service analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/uscRptEndOfService.ascx(.cs).
 *
 * ATYPICAL FILTER PANEL: legacy filters by a Payroll Calendar Month/Year (defaulting to LAST month when
 * blank), not a free date range -- the shared panel's date range is switched off and Month/Year plus
 * Payroll Area are projected into its <ng-content> slot.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './end-of-service-report.component.html',
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
export class EndOfServiceReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EndOfServiceReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    /** Legacy txtMonth/txtYear. Blank defaults (server-side) to LAST month. */
    readonly month = signal<number | undefined>(undefined);
    readonly year = signal<number | undefined>(undefined);

    /** Legacy cmbPayrollArea selected Code. Blank means "all payroll areas". */
    readonly payrollAreaCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    readonly columns: EndOfServiceReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'joiningDate', header: this.l('JoiningDate'), sortable: false, width: 140, isDate: true },
        { field: 'separation', header: this.l('Separation'), sortable: false, width: 120, isBool: true },
        { field: 'fnfPayment', header: this.l('FnfPayment'), sortable: false, width: 130, isBool: true },
        { field: 'basicSalary', header: this.l('BasicSalary'), sortable: false, width: 140 },
        { field: 'averageCommission', header: this.l('AverageCommission'), sortable: false, width: 160 },
        { field: 'averageTicket', header: this.l('AverageTicket'), sortable: false, width: 150 },
        { field: 'averageBonus', header: this.l('AverageBonus'), sortable: false, width: 150 },
        { field: 'monthlyIndemnityAmount', header: this.l('MonthlyIndemnityAmount'), sortable: false, width: 180 },
        { field: 'dailyIndemnityAmount', header: this.l('DailyIndemnityAmount'), sortable: false, width: 170 },
        { field: 'glProvisionAmount', header: this.l('GLProvisionAmount'), sortable: false, width: 160 },
        { field: 'loanAmount', header: this.l('LoanAmount'), sortable: false, width: 140 },
        { field: 'netAvailableBalance', header: this.l('NetAvailableBalance'), sortable: false, width: 170 },
    ];

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getEndOfServiceReport(
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
            .getEndOfServiceReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEndOfServiceReportInput {
        return new GetEndOfServiceReportInput({
            // Deliberately not sent: legacy filters by Month/Year, not a free date range.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            month: this.month(),
            year: this.year(),
            payrollAreaCode: this.payrollAreaCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
