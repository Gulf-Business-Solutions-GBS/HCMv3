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
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    GetMonthlyAttWithToleranceReportInput,
    MonthlyAttWithToleranceReportServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface MonthlyAttWithToleranceReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Monthly Attendance With Tolerance analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptMonthlyAttWithTolerance.ascx(.cs).
 *
 * The report window is a MONTH, not a date range: legacy has txtMonth/txtYear boxes defaulting to
 * today, so the shared date-range pickers are switched off and month/year are projected instead.
 *
 * LEGACY QUIRKS REPRODUCED, both pinned by backend tests:
 *   - a blank employee-code box returns NOTHING at all (legacy returns an empty table);
 *   - the cmbPayrollArea combo is rendered by the legacy .ascx but its filter is commented out in
 *     Table1(), so it is not offered here rather than being silently invented.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack,
 * the ColumnChooser JSON persistence, the grid per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './monthly-att-with-tolerance-report.component.html',
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
export class MonthlyAttWithToleranceReportComponent extends AppComponentBase {
    private readonly _reportService = inject(MonthlyAttWithToleranceReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- shared filter state (mirrors the backend GridReportInputBase) ---
    readonly employeeCode = signal<string>('');

    /** Legacy txtMonth, which defaulted to the current month. */
    readonly month = signal<number | undefined>(new Date().getMonth() + 1);

    /** Legacy txtYear, which defaulted to the current year. */
    readonly year = signal<number | undefined>(new Date().getFullYear());

    readonly exporting = signal<boolean>(false);

    /** Legacy grid columns, in legacy column order. */
    readonly columns: MonthlyAttWithToleranceReportColumn[] = [
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
        { field: 'da', header: this.l('DailyAdjustment'), sortable: false, width: 170 },
        { field: 'ma', header: this.l('MonthlyAccumulation'), sortable: false, width: 200 },
        { field: 'deduction', header: this.l('Deduction'), sortable: false, width: 130 },
        { field: 'systemRemarks', header: this.l('SystemRemarks'), sortable: false, width: 280 },
    ];

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getMonthlyAttWithToleranceReport(
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

    /**
     * Export honours the CURRENT FILTERS and returns EVERY matching row, not just the visible page --
     * the server ignores the paging arguments on the export endpoint.
     */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getMonthlyAttWithToleranceReportToExcel(
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
    ): GetMonthlyAttWithToleranceReportInput {
        return new GetMonthlyAttWithToleranceReportInput({
            // Present-but-undefined on purpose: NSwag types these as `DateTime | undefined`, i.e.
            // present-but-undefined and NOT optional, so they must still be listed even though this
            // screen has no date range.
            fromDate: undefined,
            toDate: undefined,
            employeeCode: this.employeeCode(),
            // Present-but-undefined on purpose: NSwag types this as `EmployeeStatus | undefined`, not
            // as optional, so it must still be listed even though this screen has no status filter.
            employeeStatus: undefined,
            month: this.month(),
            year: this.year(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
