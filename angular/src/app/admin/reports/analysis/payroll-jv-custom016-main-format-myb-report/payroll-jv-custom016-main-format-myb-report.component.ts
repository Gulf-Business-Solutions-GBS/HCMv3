import { Component, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Select } from 'primeng/select';
import { Table, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    GetPayrollJVCustom016MainFormatReportInput,
    PayrollJVCustom016MainFormatMybReportServiceProxy,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Payroll JV (Custom 016) "Main Format-New" (MYB payroll area) analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom016.ascx(.cs),
 * payroll-area-Name-contains-"MYB" branch, IsDetail == false column set (lines 1578-2016).
 *
 * PAYROLL AREA IS NOT A FILTER: this report is locked server-side to the payroll area whose Name contains
 * "MYB". See the app service remarks. Plain "Main Format" text on MYB produces no legacy output at all,
 * so only "Main Format-New" is meaningfully covered here.
 */
@Component({
    templateUrl: './payroll-jv-custom016-main-format-myb-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        Select,
        Tooltip,
        GridReportFilterComponent,
        LocalizePipe,
        DatePipe,
    ],
})
export class PayrollJVCustom016MainFormatMybReportComponent extends AppComponentBase {
    private readonly _reportService = inject(PayrollJVCustom016MainFormatMybReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- report-specific filters (legacy txtMonth/txtYear/cmbRunType) ---
    readonly month = signal<number | undefined>(new Date().getMonth() + 1);
    readonly year = signal<number | undefined>(new Date().getFullYear());
    readonly payrollRunType = signal<PayrollRunType | undefined>(undefined);

    /** Legacy employee-code box; comma-separated codes. */
    readonly employeeCode = signal<string>('');

    readonly runTypeOptions = [
        { label: this.l('Regular'), value: PayrollRunType.Regular },
        { label: this.l('OffCycle'), value: PayrollRunType.OffCycle },
    ];

    readonly exporting = signal<boolean>(false);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getPayrollJVCustom016MainFormatMybReport(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items || []);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    /** Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page. */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getPayrollJVCustom016MainFormatMybReportToExcel(
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
    ): GetPayrollJVCustom016MainFormatReportInput {
        return new GetPayrollJVCustom016MainFormatReportInput({
            // Present-but-undefined on purpose: NSwag types these as `T | undefined`, not optional, so
            // they must still be listed even though this screen has no date range and no status combo.
            fromDate: undefined,
            toDate: undefined,
            employeeStatus: undefined,
            employeeCode: this.employeeCode(),
            month: this.month(),
            year: this.year(),
            payrollRunType: this.payrollRunType(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
