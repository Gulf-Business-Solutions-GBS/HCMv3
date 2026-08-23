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
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EmployeeStatus,
    GetSalaryBenefitsReportInput,
    SalaryBenefitsReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Salary Benefits analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptSalaryBenefits.ascx(.cs).
 *
 * ATYPICAL FOR THIS FAMILY -- the COLUMN SET IS DYNAMIC. Legacy added one grid column per wage type
 * present in any basic pay current today (uscRptSalaryBenefits.ascx.cs:549-556/664-667). The server
 * returns that column list as `wageTypeNames` alongside the rows and this screen renders one column per
 * entry; each row's amounts arrive in `wageAmounts`, keyed by the same names.
 *
 * The date range is switched off on the shared filter panel: legacy pins every date-effective lookup on
 * this report to "today" (its local EndDate variable, line 518) and never reads the pickers. Legacy's
 * cmbPayrollArea is projected into the panel's <ng-content> slot as a code text box (the backend matches
 * on the PayrollArea CODE, line 541).
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-down stack (its Table2/Table3 branches are
 * commented out in legacy itself), the ColumnChooser JSON persistence, and the PDF export button.
 */
@Component({
    templateUrl: './salary-benefits-report.component.html',
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
        DecimalPipe,
    ],
})
export class SalaryBenefitsReportComponent extends AppComponentBase {
    private readonly _reportService = inject(SalaryBenefitsReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    /** Legacy cmbPayrollArea -- matched on the payroll area CODE. */
    readonly payrollAreaCode = signal<string>('');

    /** The data-driven wage-type column list for the current result set. */
    readonly wageTypeNames = signal<string[]>([]);

    readonly exporting = signal<boolean>(false);

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getSalaryBenefitsReport(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.wageTypeNames.set(result.wageTypeNames ?? []);
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    /** Reads one dynamic wage-type cell; missing means the employee has no current basic pay. */
    wageAmount(record: { wageAmounts?: { [key: string]: number } }, wageTypeName: string): number | undefined {
        return record.wageAmounts ? record.wageAmounts[wageTypeName] : undefined;
    }

    /** Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page. */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getSalaryBenefitsReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetSalaryBenefitsReportInput {
        // fromDate/toDate deliberately omitted: this report is "as of today" by construction.
        return new GetSalaryBenefitsReportInput({
            // This report has no date window (every lookup is pinned to today, as in legacy), but NSwag
            // emits fromDate/toDate as `T | undefined` -- present-but-undefined -- so the keys are required.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            payrollAreaCode: this.payrollAreaCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
