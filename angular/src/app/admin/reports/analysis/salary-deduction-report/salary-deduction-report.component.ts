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
import { GetSalaryDeductionReportInput, SalaryDeductionReportServiceProxy } from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Salary Deduction grid report.
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryDeduction.ascx(.cs).
 *
 * DATA-DRIVEN COLUMNS: the earning/deduction wage-type columns are not fixed -- see the backend
 * SalaryDeductionReportOutput remarks. This component renders them from the returned earningColumns /
 * deductionColumns lists, reading each row's value out of its wageTypeAmounts dictionary.
 */
@Component({
    templateUrl: './salary-deduction-report.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, TableModule, PaginatorModule, GridReportFilterComponent, LocalizePipe],
})
export class SalaryDeductionReportComponent extends AppComponentBase {
    private readonly _reportService = inject(SalaryDeductionReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    month: number | undefined = new Date().getMonth() + 1;
    year: number | undefined = new Date().getFullYear();
    payrollAreaId: number | undefined = undefined;

    readonly earningColumns = signal<string[]>([]);
    readonly deductionColumns = signal<string[]>([]);

    exporting = false;

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getSalaryDeductionReport(
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
                this.earningColumns.set(result.earningColumns || []);
                this.deductionColumns.set(result.deductionColumns || []);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    exportToExcel(): void {
        this.exporting = true;

        this._reportService
            .getSalaryDeductionReportToExcel(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage,
                    0
                )
            )
            .pipe(finalize(() => (this.exporting = false)))
            .subscribe((result) => {
                this._fileDownloadService.downloadTempFile(result);
            });
    }

    wageAmount(record: any, columnName: string): number {
        return record?.wageTypeAmounts ? record.wageTypeAmounts[columnName] || 0 : 0;
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetSalaryDeductionReportInput {
        return new GetSalaryDeductionReportInput({
            fromDate: undefined,
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            month: this.month,
            year: this.year,
            payrollAreaId: this.payrollAreaId,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
