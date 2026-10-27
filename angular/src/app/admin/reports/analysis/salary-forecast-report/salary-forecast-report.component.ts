import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
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
    GetSalaryForecastReportInput,
    SalaryForecastReportServiceProxy,
    SalaryForecastType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Salary Forecast grid report.
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryForecast.ascx(.cs).
 * Only the Company/Department roll-up (legacy Table1) is ported -- the employee-level drill-down (Table2)
 * is unreachable without the drill-down session stack this report family does not port.
 */
@Component({
    templateUrl: './salary-forecast-report.component.html',
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
export class SalaryForecastReportComponent extends AppComponentBase {
    private readonly _reportService = inject(SalaryForecastReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    fromDate: Date | undefined = new Date();
    forecastType: SalaryForecastType | undefined = undefined;
    salaryChangePercent: number | undefined = 0;

    readonly forecastTypeOptions = [
        { label: this.l('All'), value: SalaryForecastType.All },
        { label: this.l('BasicPay'), value: SalaryForecastType.BasicPay },
    ];

    readonly forecastYears = signal<number[]>([]);

    exporting = false;

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getSalaryForecastReport(
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
                this.forecastYears.set(result.forecastYears || []);
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
            .getSalaryForecastReportToExcel(
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

    forecastAmount(record: any, index: number): number {
        return record?.forecastAmounts ? record.forecastAmounts[index] || 0 : 0;
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetSalaryForecastReportInput {
        return new GetSalaryForecastReportInput({
            fromDate: this.fromDate ? DateTime.fromJSDate(this.fromDate) : undefined,
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            forecastType: this.forecastType,
            salaryChangePercent: this.salaryChangePercent,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
