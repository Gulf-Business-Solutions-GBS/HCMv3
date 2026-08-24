import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
    GetMonthlyAttWithToleranceCustom019ReportInput,
    MonthlyAttWithToleranceCustom019ReportServiceProxy,
    MonthlyAttWithToleranceCustom019Variant,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface MonthlyAttWithToleranceCustom019ReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Monthly Attendance With Tolerance (Custom 019) analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyAttWithTolerance_Custom019.ascx(.cs).
 *
 * The report window is a MONTH, not a date range, so the shared date-range pickers are switched off and
 * month/year are projected into the shared filter's <ng-content> slot -- as are this variant's four extra
 * controls: Varient, Department, Division and Section. The shared <grid-report-filter> is reused, never
 * forked.
 *
 * DIFFERENT FROM THE BASE SCREEN, all pinned by backend tests:
 *   - a blank employee-code box is now ALLOWED (the base returns nothing at all); the three org filters
 *     are the customer's way of bounding an otherwise company-wide run;
 *   - the DA / MA / Deduction trio is replaced by DailyDeduction / RunningDeduction, and the boolean
 *     Deduction column is gone entirely;
 *   - an extra MONTHLY DEDUCTION ROW per employee appears on the last day of the month, recognisable by
 *     its "Monthly LateIn/EarlyOut Deduction" status;
 *   - the Summary variant shows ONLY those monthly rows, with the day-level columns hidden -- exactly
 *     the columns legacy's keepColumns array leaves behind.
 *
 * cmbPayrollArea is on the legacy panel but its filter block does not exist in Table1(), so it is not
 * offered here rather than being silently invented.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack, the
 * ColumnChooser JSON persistence, the grid per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './monthly-att-with-tolerance-custom019-report.component.html',
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
export class MonthlyAttWithToleranceCustom019ReportComponent extends AppComponentBase {
    private readonly _reportService = inject(MonthlyAttWithToleranceCustom019ReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- shared filter state (mirrors the backend GridReportInputBase) ---
    readonly employeeCode = signal<string>('');

    /** Legacy txtMonth, which defaulted to the current month. */
    readonly month = signal<number | undefined>(new Date().getMonth() + 1);

    /** Legacy txtYear, which defaulted to the current year. */
    readonly year = signal<number | undefined>(new Date().getFullYear());

    /** Legacy cmbDepartment -- matched against the employee's OrgLevel2 code. */
    readonly departmentCode = signal<string | undefined>(undefined);

    /** Legacy cmbDivision -- matched against the employee's OrgLevel3 code. */
    readonly divisionCode = signal<string | undefined>(undefined);

    /** Legacy cmbSection -- matched against the employee's OrgLevel4 code. */
    readonly sectionCode = signal<string | undefined>(undefined);

    /** Legacy cmbVarient, whose two items are Detail (default) and Summary. */
    readonly variant = signal<MonthlyAttWithToleranceCustom019Variant>(
        MonthlyAttWithToleranceCustom019Variant.Detail
    );

    readonly variantOptions = [
        { label: this.l('Detail'), value: MonthlyAttWithToleranceCustom019Variant.Detail },
        { label: this.l('Summary'), value: MonthlyAttWithToleranceCustom019Variant.Summary },
    ];

    readonly exporting = signal<boolean>(false);

    /**
     * Legacy grid columns, in legacy DataTable order (Detail variant).
     *
     * Oid is not shown: on this variant it carries the employee id, which the Code column already
     * identifies far more usefully.
     */
    private readonly detailColumns: MonthlyAttWithToleranceCustom019ReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'dayDate', header: this.l('Date'), sortable: true, width: 130, isDate: true },
        { field: 'weekDay', header: this.l('WeekDay'), sortable: false, width: 130 },
        { field: 'shiftStart', header: this.l('ShiftStart'), sortable: false, width: 130 },
        { field: 'shiftEnd', header: this.l('ShiftEnd'), sortable: false, width: 130 },
        { field: 'signIn', header: this.l('SignIn'), sortable: false, width: 120 },
        { field: 'signOut', header: this.l('SignOut'), sortable: false, width: 120 },
        { field: 'leaveMins', header: this.l('LeaveMins'), sortable: false, width: 130 },
        { field: 'earlyIn', header: this.l('EarlyIn'), sortable: false, width: 120 },
        { field: 'lateIn', header: this.l('LateIn'), sortable: false, width: 120 },
        { field: 'earlyOut', header: this.l('EarlyOut'), sortable: false, width: 120 },
        { field: 'lateOut', header: this.l('LateOut'), sortable: false, width: 120 },
        { field: 'workingMins', header: this.l('WorkingMins'), sortable: false, width: 150 },
        { field: 'plannedMins', header: this.l('PlannedMins'), sortable: false, width: 150 },
        { field: 'dailyDeduction', header: this.l('DailyDeduction'), sortable: false, width: 170 },
        { field: 'runningDeduction', header: this.l('RunningDeduction'), sortable: false, width: 190 },
        { field: 'systemRemarks', header: this.l('Status'), sortable: false, width: 280 },
    ];

    /**
     * The Summary variant's reduced column set -- legacy's keepColumns array (lines 1653-1665) minus the
     * Oid column. Note it keeps Early In and Late Out even though the monthly row never fills them; that
     * is legacy's list, reproduced.
     */
    private readonly summaryColumns: MonthlyAttWithToleranceCustom019ReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'earlyIn', header: this.l('EarlyIn'), sortable: false, width: 120 },
        { field: 'lateIn', header: this.l('LateIn'), sortable: false, width: 120 },
        { field: 'earlyOut', header: this.l('EarlyOut'), sortable: false, width: 120 },
        { field: 'lateOut', header: this.l('LateOut'), sortable: false, width: 120 },
        { field: 'dailyDeduction', header: this.l('DailyDeduction'), sortable: false, width: 170 },
        { field: 'runningDeduction', header: this.l('RunningDeduction'), sortable: false, width: 190 },
        { field: 'systemRemarks', header: this.l('Status'), sortable: false, width: 280 },
    ];

    /**
     * Tracks the variant of the LAST completed fetch, not the combo, so the grid's columns never get
     * ahead of the rows that are actually in it.
     */
    private readonly loadedVariant = signal<MonthlyAttWithToleranceCustom019Variant>(
        MonthlyAttWithToleranceCustom019Variant.Detail
    );

    readonly columns = computed(() =>
        this.loadedVariant() === MonthlyAttWithToleranceCustom019Variant.Summary
            ? this.summaryColumns
            : this.detailColumns
    );

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        const requestedVariant = this.variant();

        this._reportService
            .getMonthlyAttWithToleranceCustom019Report(
                this.buildInput(
                    this.primengTableHelper.getSorting(this.dataTable()),
                    this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    this.primengTableHelper.getSkipCount(this.paginator(), event)
                )
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.loadedVariant.set(requestedVariant);
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    /**
     * Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page --
     * the server ignores the paging arguments on the export endpoint.
     */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getMonthlyAttWithToleranceCustom019ReportToExcel(
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
    ): GetMonthlyAttWithToleranceCustom019ReportInput {
        return new GetMonthlyAttWithToleranceCustom019ReportInput({
            // Present-but-undefined on purpose: NSwag types these as `DateTime | undefined`, i.e.
            // present-but-undefined and NOT optional, so they must still be listed even though this
            // screen has no date range.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            // Present-but-undefined on purpose: NSwag types this as `EmployeeStatus | undefined`, not as
            // optional, so it must still be listed even though this screen has no status filter.
            employeeStatus: undefined,
            month: this.month(),
            year: this.year(),
            departmentCode: this.departmentCode(),
            divisionCode: this.divisionCode(),
            sectionCode: this.sectionCode(),
            variant: this.variant(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
