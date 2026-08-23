import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EmployeeSalaryChangeComponentDto,
    EmployeeSalaryChangeReportServiceProxy,
    EmployeeStatus,
    GetEmployeeSalaryChangeReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Employee Salary Change analysis grid report.
 *
 * ATYPICAL: this report is a PIVOT -- its wage-type columns come from the data, not from a fixed list, so
 * the header row is built at runtime from the `components` block that every row carries. Legacy showed
 * only the date range on this screen (there is no employee-code or status control in
 * uscRptEmployeeSalaryChange.ascx), so those two shared filters are switched off.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptEmployeeSalaryChange.ascx(.cs).
 *
 * Uses the shared <grid-report-filter> panel unchanged: legacy shows exactly the four common filters on
 * this screen and nothing report-specific.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid's per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './employee-salary-change-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Tooltip,
        GridReportFilterComponent,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class EmployeeSalaryChangeReportComponent extends AppComponentBase {
    private readonly _reportService = inject(EmployeeSalaryChangeReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- shared filter state (mirrors the backend GridReportInputBase) ---
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    /**
     * The dynamic wage-type column list. Identical, and identically ordered, on every row, so the first
     * row defines the header; an empty page leaves it empty and only the fixed columns render.
     */
    readonly components = signal<EmployeeSalaryChangeComponentDto[]>([]);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getEmployeeSalaryChangeReport(
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
                this.components.set(result.items.length ? (result.items[0].components ?? []) : []);
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
            .getEmployeeSalaryChangeReportToExcel(
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

    /** EmployeeStatus is generated as a NUMERIC enum, so the grid must map it to text itself. */
    employeeStatusLabel(status: EmployeeStatus): string {
        return status === EmployeeStatus.Active ? this.l('Active') : this.l('Inactive');
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetEmployeeSalaryChangeReportInput {
        return new GetEmployeeSalaryChangeReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
