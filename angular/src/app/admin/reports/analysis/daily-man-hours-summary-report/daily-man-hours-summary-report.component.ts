import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { DatePicker } from 'primeng/datepicker';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Select } from 'primeng/select';
import { Table, TableModule } from 'primeng/table';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    DailyManHoursSummaryReportServiceProxy,
    GetAllWorkLocationsInput,
    GetDailyManHoursSummaryDetailReportInput,
    GetDailyManHoursSummaryReportInput,
    ManHourType,
    WorkLocationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';
import { injectOrgLevelCaptions } from '../shared/org-level-captions';

interface DailyManHoursSummaryReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
}

/**
 * Daily Man Hours Summary analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptDailyManHoursSummary.ascx(.cs).
 *
 * ATYPICAL vs the reference screen (employee-contract-report) in three ways.
 *
 * 1. ONE DATE, NOT A RANGE. The legacy panel carries a single dtFrom ("effective date") plus
 *    cmbWorkLocation, and no dtTo / employee-code box / status combo. Those three are switched off with
 *    the shared panel's existing [show*] inputs; the effective date and the work location are projected
 *    through <ng-content /> rather than forking the shared component.
 *
 * 2. REAL TWO-LEVEL DRILL-DOWN. Legacy's Table1 is two fixed rows (Regular / Overtime) with a headcount
 *    each; clicking one rendered the employees behind that count.
 *
 * 3. TWO DIFFERENT DETAIL SHAPES. Regular and Overtime do not share a grid: Regular shows sign in /
 *    sign out / total hours, Overtime shows overtime and accumulated overtime hours. They are separate
 *    server endpoints and separate column sets here, selected by the drilled-into type.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the session-based drill stack
 * itself, the ColumnChooser JSON persistence, the grid per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './daily-man-hours-summary-report.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        DatePicker,
        Select,
        GridReportFilterComponent,
        LocalizePipe,
    ],
})
export class DailyManHoursSummaryReportComponent extends AppComponentBase implements OnInit {
    private readonly _reportService = inject(DailyManHoursSummaryReportServiceProxy);
    private readonly _workLocationService = inject(WorkLocationsServiceProxy);
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

    /** Legacy cmbWorkLocation. Holds WorkLocation.Code. */
    readonly workLocationCode = signal<string | undefined>(undefined);
    readonly workLocationCodeOptions = signal<{ label: string; value: string }[]>([]);

    readonly exporting = signal<boolean>(false);

    // --- drill-down state (legacy: Session[indexKey] + selectedColumnValue) ---
    readonly selectedType = signal<ManHourType | undefined>(undefined);
    readonly selectedTypeName = signal<string>('');

    readonly detailRecords = signal<any[]>([]);
    readonly detailTotalCount = signal<number>(0);
    readonly detailLoading = signal<boolean>(false);

    /** Legacy Table1 columns. */
    readonly masterColumns: DailyManHoursSummaryReportColumn[] = [
        { field: 'typeName', header: this.l('ManHourType'), sortable: true, width: 320 },
        { field: 'employeeCount', header: this.l('Employees'), sortable: true, width: 180 },
    ];

    /** OrgLevel1..7 are captioned with the tenant's Organization Unit Type names; undefined levels are hidden. */
    readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
    private readonly detailIdentityColumns: DailyManHoursSummaryReportColumn[] = [
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

    /** The drilled-into level's columns: Regular and Overtime have genuinely different grids. */
    get detailColumns(): DailyManHoursSummaryReportColumn[] {
        if (this.selectedType() === ManHourType.Overtime) {
            return [
                ...this.orgLevels.apply(this.detailIdentityColumns),
                { field: 'overtimeHours', header: this.l('OvertimeHours'), sortable: false, width: 160 },
                {
                    field: 'accumulatedOvertimeHours',
                    header: this.l('AccumulatedOTHours'),
                    sortable: false,
                    width: 200,
                },
            ];
        }

        return [
            ...this.orgLevels.apply(this.detailIdentityColumns),
            { field: 'signIn', header: this.l('SignIn'), sortable: false, width: 120 },
            { field: 'signOut', header: this.l('SignOut'), sortable: false, width: 120 },
            { field: 'totalHours', header: this.l('TotalHours'), sortable: false, width: 150 },
        ];
    }

    /**
     * Loads ONLY the report-specific lookup. The grid itself is still not fetched here -- the table's
     * (onLazyLoad) fires the first getRecords().
     */
    ngOnInit(): void {
        this._workLocationService
            .getAll(
                new GetAllWorkLocationsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.workLocationCodeOptions.set(
                    result.items.map((item) => ({
                        label: item.nameEn ?? item.code ?? '',
                        value: item.code ?? '',
                    }))
                );
            });
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._reportService
            .getDailyManHoursSummaryReport(
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

    /** Legacy drill-down: clicking a master row rendered the employees behind that count. */
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
        const type = this.selectedType();
        if (type == null) {
            return;
        }

        const input = this.buildDetailInput(
            this.primengTableHelper.getSorting(this.detailTable()),
            this.primengTableHelper.getMaxResultCount(this.detailPaginator(), event),
            this.primengTableHelper.getSkipCount(this.detailPaginator(), event)
        );

        this.detailLoading.set(true);

        const request: Observable<{ totalCount: number; items: any[] }> =
            type === ManHourType.Overtime
                ? this._reportService.getDailyManHoursOvertimeDetailReport(input)
                : this._reportService.getDailyManHoursRegularDetailReport(input);

        request.pipe(finalize(() => this.detailLoading.set(false))).subscribe((result) => {
            this.detailTotalCount.set(result.totalCount);
            this.detailRecords.set(result.items);
        });
    }

    /** Exports the MASTER level -- every matching row, not just the visible page. */
    exportToExcel(): void {
        this.exporting.set(true);

        this._reportService
            .getDailyManHoursSummaryReportToExcel(
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
        const type = this.selectedType();
        if (type == null) {
            return;
        }

        const input = this.buildDetailInput(
            this.primengTableHelper.getSorting(this.detailTable()),
            this.detailPaginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage,
            0
        );

        this.exporting.set(true);

        const request =
            type === ManHourType.Overtime
                ? this._reportService.getDailyManHoursOvertimeDetailReportToExcel(input)
                : this._reportService.getDailyManHoursRegularDetailReportToExcel(input);

        request.pipe(finalize(() => this.exporting.set(false))).subscribe((result) => {
            this._fileDownloadService.downloadTempFile(result);
        });
    }

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetDailyManHoursSummaryReportInput {
        return new GetDailyManHoursSummaryReportInput({
            fromDate: this.effectiveDate() ? DateTime.fromJSDate(this.effectiveDate()) : undefined,
            // Present-but-undefined on purpose: NSwag types these as `T | undefined`, i.e.
            // present-but-undefined and NOT optional, so they must still be listed even though this
            // screen has no To date, no employee-code box and no status combo.
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            workLocationCode: this.workLocationCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }

    private buildDetailInput(
        sorting: string,
        maxResultCount: number,
        skipCount: number
    ): GetDailyManHoursSummaryDetailReportInput {
        return new GetDailyManHoursSummaryDetailReportInput({
            fromDate: this.effectiveDate() ? DateTime.fromJSDate(this.effectiveDate()) : undefined,
            toDate: undefined,
            employeeCode: undefined,
            employeeStatus: undefined,
            workLocationCode: this.workLocationCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
