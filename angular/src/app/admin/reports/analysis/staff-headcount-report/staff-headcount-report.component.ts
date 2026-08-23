import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    EmployeeStatus,
    GetStaffHeadcountEmployeeReportInput,
    GetStaffHeadcountReportInput,
    StaffHeadcountReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

/**
 * Staff Headcount analysis grid report.
 *
 * Ported legacy screen: HCM10.Web/UserControls/Reports/Analysis/Employees/uscRptStaffHeadcount.ascx(.cs).
 *
 * ATYPICAL FOR THIS FAMILY -- this is a real DRILL-DOWN report. Legacy had three levels (department
 * headcount -> employees of that department -> full employee record) driven by a session stack; this
 * screen ports the first two and swaps the session stack for explicit component state plus a "back"
 * button. Legacy's level 3 is not ported -- it re-emits the whole employee master record and is
 * column-for-column the separate uscRptEmployeeInfo report (see the app service remarks).
 *
 * The date range is switched off on the shared filter panel: legacy's Table1/Table2 apply no date
 * predicate at all (uscRptStaffHeadcount.ascx.cs:476-491).
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing): the drill-up session stack itself, the ColumnChooser
 * JSON persistence, and the PDF export button.
 */
@Component({
    templateUrl: './staff-headcount-report.component.html',
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
    ],
})
export class StaffHeadcountReportComponent extends AppComponentBase {
    private readonly _reportService = inject(StaffHeadcountReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    readonly exporting = signal<boolean>(false);

    /** Null while showing level 1 (departments); set to the drilled-into row while showing level 2. */
    readonly selectedOrganizationUnitId = signal<number | undefined>(undefined);
    readonly selectedDepartmentName = signal<string>('');

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        const sorting = this.primengTableHelper.getSorting(this.dataTable());
        const maxResultCount = this.primengTableHelper.getMaxResultCount(this.paginator(), event);
        const skipCount = this.primengTableHelper.getSkipCount(this.paginator(), event);

        // Typed explicitly: the ternary below yields a union of two DIFFERENT PagedResultDto observables,
        // and TypeScript will not unify their overloaded pipe/subscribe signatures. Both branches share
        // this shape, which is all this method consumes.
        const request: Observable<{ totalCount: number; items: unknown[] }> = this.isDrilledDown()
            ? (this._reportService.getStaffHeadcountEmployeeReport(
                  this.buildEmployeeInput(sorting, maxResultCount, skipCount)
              ) as unknown as Observable<{ totalCount: number; items: unknown[] }>)
            : (this._reportService.getStaffHeadcountReport(this.buildInput(sorting, maxResultCount, skipCount)) as unknown as Observable<{ totalCount: number; items: unknown[] }>);

        request
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result: { totalCount: number; items: unknown[] }) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    isDrilledDown(): boolean {
        return this.selectedOrganizationUnitId() !== undefined;
    }

    /** Legacy's drill-down: clicking a level-1 row opens the employees behind that headcount. */
    drillDown(record: { id: number; departmentName: string }): void {
        this.selectedOrganizationUnitId.set(record.id);
        this.selectedDepartmentName.set(record.departmentName);
        this.resetSorting();
        this.reloadPage();
    }

    /** Legacy's DrillUp button. */
    drillUp(): void {
        this.selectedOrganizationUnitId.set(undefined);
        this.selectedDepartmentName.set('');
        this.resetSorting();
        this.reloadPage();
    }

    /**
     * The two drill levels have DIFFERENT columns, so a sort left over from one level would be sent as a
     * field the other level's DTO does not have and the server-side dynamic OrderBy would throw. Clearing
     * it makes each level fall back to its own DefaultSorting.
     */
    private resetSorting(): void {
        const table = this.dataTable();
        if (table) {
            table.multiSortMeta = undefined;
            table.sortField = undefined;
            table.sortOrder = 1;
        }
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: 0, rows } as LazyLoadEvent);
    }

    /** Export honours the CURRENT FILTERS and the current drill level, and returns EVERY matching row. */
    exportToExcel(): void {
        this.exporting.set(true);

        const sorting = this.primengTableHelper.getSorting(this.dataTable());
        const rows = this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage;

        const request = this.isDrilledDown()
            ? this._reportService.getStaffHeadcountEmployeeReportToExcel(this.buildEmployeeInput(sorting, rows, 0))
            : this._reportService.getStaffHeadcountReportToExcel(this.buildInput(sorting, rows, 0));

        request.pipe(finalize(() => this.exporting.set(false))).subscribe((result) => {
            this._fileDownloadService.downloadTempFile(result);
        });
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetStaffHeadcountReportInput {
        // Legacy applies no date predicate on this report, but NSwag emits fromDate/toDate as
        // `T | undefined` -- present-but-undefined, not optional -- so the keys must still be supplied.
        return new GetStaffHeadcountReportInput({
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }

    private buildEmployeeInput(
        sorting: string,
        maxResultCount: number,
        skipCount: number
    ): GetStaffHeadcountEmployeeReportInput {
        return new GetStaffHeadcountEmployeeReportInput({
            fromDate: undefined,
            toDate: undefined,
            organizationUnitId: this.selectedOrganizationUnitId(),
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
