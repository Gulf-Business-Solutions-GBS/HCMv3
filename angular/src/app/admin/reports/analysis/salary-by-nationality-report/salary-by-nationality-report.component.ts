import { DecimalPipe } from '@angular/common';
import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import {
    EmployeeStatus,
    GetSalaryByNationalityReportInput,
    SalaryByNationalityReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Salary By Nationality analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptSalaryByNationality.ascx(.cs).
 *
 * AGGREGATE report -- one row per nationality, not per employee.
 *
 * The shared filter panel's date range and employee code are switched OFF rather than forked away:
 * legacy has no such inputs on this report and the query is pinned to "today"
 * (uscRptSalaryByNationality.ascx.cs:527-529). The report-specific cmbNationality is projected into the
 * panel's <ng-content> slot as a code text box (the backend matches on the Nationality CODE, line 545).
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-down stack (its "Table2" branch is commented
 * out in legacy itself), the ColumnChooser JSON persistence, and the PDF export button.
 */
@Component({
    templateUrl: './salary-by-nationality-report.component.html',
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
        DecimalPipe,
    ],
})
export class SalaryByNationalityReportComponent extends AppComponentBase {
    private readonly _reportService = inject(SalaryByNationalityReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    /** Legacy cmbNationality -- matched on the nationality CODE. */
    readonly nationalityCode = signal<string>('');

    readonly exporting = signal<boolean>(false);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getSalaryByNationalityReport(
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

    /** Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page. */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getSalaryByNationalityReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetSalaryByNationalityReportInput {
        // fromDate/toDate/employeeCode are deliberately omitted: this report has no such filters.
        return new GetSalaryByNationalityReportInput({
            // Legacy has no date or employee-code inputs on this screen; NSwag still requires the keys.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: this.employeeStatus(),
            nationalityCode: this.nationalityCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
