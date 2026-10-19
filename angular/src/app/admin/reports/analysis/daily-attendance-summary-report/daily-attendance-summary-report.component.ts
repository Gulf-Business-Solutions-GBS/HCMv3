import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { DatePicker } from 'primeng/datepicker';
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
    DailyAttendanceSummaryReportServiceProxy,
    DailyAttendanceType,
    GetDailyAttendanceSummaryDetailReportInput,
    GetDailyAttendanceSummaryReportInput,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface DailyAttendanceSummaryReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
}

/**
 * Daily Attendance Summary analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyAttendanceSummary.ascx(.cs).
 *
 * ATYPICAL vs the reference screen (employee-contract-report) in two ways.
 *
 * 1. ONE DATE, NOT A RANGE. The legacy panel carries a single dtFrom ("effective date") and no dtTo, no
 *    employee-code box and no status combo -- the report always counts ACTIVE employees on one day. So
 *    the shared panel's date range, code box and status combo are all switched off with the existing
 *    [showDateRange]/[showEmployeeCode]/[showEmployeeStatus] inputs and a single effective-date picker
 *    is projected through <ng-content /> instead of forking the shared component.
 *
 * 2. REAL TWO-LEVEL DRILL-DOWN. Legacy's Table1 is five fixed rows (On Duty / Day Off / Medical /
 *    On Leave / Absent) with a headcount each; clicking one rendered Table2, the employees behind that
 *    count. Here the master table's row click swaps in the detail table (own paging, own export, Back
 *    button) and passes the selected type down; the same filter panel drives both levels.
 *
 * The Leave Type detail column exists ONLY in the On Leave view -- legacy adds that DataColumn
 * conditionally -- so it is appended conditionally here too rather than shown always empty.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the session-based drill stack
 * itself, the ColumnChooser JSON persistence, the grid per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './daily-attendance-summary-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        DatePicker,
        GridReportFilterComponent,
        LocalizePipe,
    ],
})
export class DailyAttendanceSummaryReportComponent extends AppComponentBase {
    private readonly _reportService = inject(DailyAttendanceSummaryReportServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');
    readonly detailTable = viewChild<Table>('detailTable');
    readonly detailPaginator = viewChild<Paginator>('detailPaginator');

    /**
     * Legacy dtFrom, the single "effective date". It is sent as the input's fromDate because that is the
     * field the server resolves; toDate is deliberately left undefined.
     */
    readonly effectiveDate = signal<Date | undefined>(new Date());

    readonly exporting = signal<boolean>(false);

    // --- drill-down state (legacy: Session[indexKey] + selectedColumnValue) ---
    readonly selectedType = signal<DailyAttendanceType | undefined>(undefined);
    readonly selectedTypeName = signal<string>('');

    readonly detailRecords = signal<any[]>([]);
    readonly detailTotalCount = signal<number>(0);
    readonly detailLoading = signal<boolean>(false);

    /** Legacy Table1 columns. */
    readonly masterColumns: DailyAttendanceSummaryReportColumn[] = [
        { field: 'typeName', header: this.l('Type'), sortable: true, width: 320 },
        { field: 'totalEmployees', header: this.l('TotalEmp'), sortable: true, width: 180 },
    ];

    /** OrgLevel1..7 are captioned with the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    /** Legacy Table2 columns. Leave Type is appended only for the On Leave drill-down. */
    get detailColumns(): DailyAttendanceSummaryReportColumn[] {
        const columns: DailyAttendanceSummaryReportColumn[] = [
            { field: 'employeeCode', header: this.l('EmployeeCode'), sortable: true, width: 130 },
            { field: 'employeeName', header: this.l('EmployeeName'), sortable: true, width: 220 },
            { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
            { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
            { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
            { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
            { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
            { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
            { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        ];

        if (this.selectedType() === DailyAttendanceType.OnLeave) {
            columns.push({ field: 'leaveTypeName', header: this.l('LeaveType'), sortable: false, width: 180 });
        }

        return this.orgLevels.apply(columns);
    }

    // No ngOnInit fetch on purpose: the table's (onLazyLoad) fires the first getRecords().

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getDailyAttendanceSummaryReport(
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
        // Re-running the filters always returns to the master level, exactly like legacy's Get Data button.
        this.selectedType.set(undefined);

        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRecords({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    /** Legacy drill-down: clicking a master row rendered Table2 for that attendance type. */
    drillDown(record: any): void {
        if (record.type == null) {
            return;
        }

        this.selectedType.set(record.type);
        this.selectedTypeName.set(record.typeName);
        this.detailRecords.set([]);
        this.detailTotalCount.set(0);
    }

    /** Legacy drill-up button. */
    drillUp(): void {
        this.selectedType.set(undefined);
        this.detailRecords.set([]);
        this.detailTotalCount.set(0);
    }

    getDetailRecords(event?: LazyLoadEvent): void {
        if (this.selectedType() == null) {
            return;
        }

        this.detailLoading.set(true);

        this._reportService
            .getDailyAttendanceSummaryDetailReport(
                this.buildDetailInput(
                    this.primengTableHelper.getSorting(this.detailTable()),
                    this.primengTableHelper.getMaxResultCount(this.detailPaginator(), event),
                    this.primengTableHelper.getSkipCount(this.detailPaginator(), event)
                )
            )
            .pipe(finalize(() => this.detailLoading.set(false)))
            .subscribe((result) => {
                this.detailTotalCount.set(result.totalCount);
                this.detailRecords.set(result.items);
            });
    }

    /** Exports the MASTER level -- every matching row, not just the visible page. */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getDailyAttendanceSummaryReportToExcel(
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

    /** Exports the DETAIL level -- every employee behind the drilled-into count, not just one page. */
    exportDetailToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getDailyAttendanceSummaryDetailReportToExcel(
                this.buildDetailInput(
                    this.primengTableHelper.getSorting(this.detailTable()),
                    this.detailPaginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage,
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
    ): GetDailyAttendanceSummaryReportInput {
        return new GetDailyAttendanceSummaryReportInput({
            fromDate: this.effectiveDate() ? DateTime.fromJSDate(this.effectiveDate()) : undefined,
            // Present-but-undefined on purpose: NSwag types these as `T | undefined`, i.e.
            // present-but-undefined and NOT optional, so they must still be listed even though this
            // screen has no To date, no employee-code box and no status combo.
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }

    private buildDetailInput(
        sorting: string,
        maxResultCount: number,
        skipCount: number
    ): GetDailyAttendanceSummaryDetailReportInput {
        return new GetDailyAttendanceSummaryDetailReportInput({
            fromDate: this.effectiveDate() ? DateTime.fromJSDate(this.effectiveDate()) : undefined,
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            type: this.selectedType(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
