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
import {
    EmployeeBonusReportServiceProxy,
    EmployeeStatus,
    GetEmployeeBonusReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/** One rendered grid column. The year columns are data-dependent, so the column list is built at runtime. */
interface BonusReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
}

/**
 * Employee Bonus analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeBonus.ascx(.cs).
 *
 * ATYPICAL vs the reference screen (employee-contract-report): this report is a PIVOT -- legacy generated
 * one column per calendar year found in the bonus transactions, so the header cannot be static HTML. The
 * server returns the year list on every response and the columns are rebuilt from it.
 *
 * The legacy date pickers are hidden on this screen (Visible="false" in the .ascx), so the shared filter
 * panel is asked for status + employee code only.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-down/drill-up stack, the ColumnChooser JSON
 * persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-bonus-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        GridReportFilterComponent,
        LocalizePipe,
    ],
})
export class EmployeeBonusReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EmployeeBonusReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- shared filter state (mirrors the backend GridReportInputBase) ---
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    readonly columns = signal<BonusReportColumn[]>(this.buildColumns([]));

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getEmployeeBonusReport(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                const years = result.years || [];
                this.columns.set(this.buildColumns(years));

                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(
                    (result.items || []).map((item) => this.flatten(item, years))
                );
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    /**
     * Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page.
     */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getEmployeeBonusReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeBonusReportInput {
        return new GetEmployeeBonusReportInput({
            // fromDate/toDate are deliberately not sent: legacy hides both pickers on this screen.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }

    /** Legacy column order: Code, Name, one column per year, Average, Monthly average. */
    private buildColumns(years: number[]): BonusReportColumn[] {
        const columns: BonusReportColumn[] = [
            { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
            { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        ];

        years.forEach((year) => {
            columns.push({ field: 'year_' + year, header: String(year), sortable: false, width: 120 });
        });

        columns.push({
            field: 'averageBonusAmount',
            header: this.l('AverageBonusAmount'),
            sortable: false,
            width: 180,
        });
        columns.push({
            field: 'monthlyAverageBonusAmount',
            header: this.l('MonthlyAverageBonusAmount'),
            sortable: false,
            width: 220,
        });

        return columns;
    }

    /** Turns the row's yearlyAmounts array into flat year_YYYY fields the generic table can bind to. */
    private flatten(item: any, years: number[]): any {
        const flat: any = { ...item };
        const amounts = item.yearlyAmounts || [];

        years.forEach((year, index) => {
            flat['year_' + year] = amounts[index] ? amounts[index].amount : 0;
        });

        return flat;
    }
}
