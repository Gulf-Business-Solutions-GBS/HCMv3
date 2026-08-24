import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
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
    EmployeeStatus,
    GetAllPayrollAreasInput,
    GetLateEarlyLogReportInput,
    LateEarlyLogReportServiceProxy,
    PayrollAreasServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { GridReportFilterComponent } from '../shared/grid-report-filter.component';

interface LateEarlyLogReportColumn {
    field: string;
    header: string;
    sortable: boolean;
    width: number;
    isDate?: boolean;
}

/**
 * Late/Early Log analysis grid report.
 *
 * Ported legacy screen:
 * HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLog.ascx(.cs).
 *
 * The widest grid in the TimeReport family (35 columns). The columns are declared once as data
 * and looped over in the template rather than written out as 35 hand-rolled <th>/<td> pairs.
 *
 * The clock columns (ShiftStart, SignIn, EarlyIn, TotalHours, ...) are .NET TimeSpans, which NSwag
 * emits as plain strings, so they are rendered as text and NOT through the luxon date pipe.
 *
 * NOT PORTED (DevExpress/WebForms UI plumbing with no data meaning): the drill-down/drill-up stack,
 * the ColumnChooser JSON persistence, the grid per-column filter row, and the PDF export button.
 */
@Component({
    templateUrl: './late-early-log-report.component.html',
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
export class LateEarlyLogReportComponent extends AppComponentBase implements OnInit {
    private readonly _reportService = inject(LateEarlyLogReportServiceProxy);
    private readonly _payrollAreaService = inject(PayrollAreasServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // --- shared filter state (mirrors the backend GridReportInputBase) ---
    readonly fromDate = signal<Date | undefined>(new Date());
    readonly toDate = signal<Date | undefined>(new Date());
    readonly employeeCode = signal<string>('');
    readonly employeeStatus = signal<EmployeeStatus | undefined>(undefined);

    /** Legacy cmbPayrollArea. Holds PayrollArea.Code, which is what the legacy combo's value was. */
    readonly payrollAreaCode = signal<string | undefined>(undefined);
    readonly payrollAreaCodeOptions = signal<{ label: string; value: string }[]>([]);

    readonly exporting = signal<boolean>(false);

    /** Legacy grid columns, in legacy column order. */
    readonly columns: LateEarlyLogReportColumn[] = [
        { field: 'employeeCode', header: this.l('Code'), sortable: true, width: 120 },
        { field: 'employeeName', header: this.l('Employee'), sortable: true, width: 220 },
        { field: 'accessId', header: this.l('AccessId'), sortable: false, width: 130 },
        { field: 'orgLevel1', header: this.l('OrgLevel1'), sortable: false, width: 160 },
        { field: 'orgLevel2', header: this.l('OrgLevel2'), sortable: false, width: 160 },
        { field: 'orgLevel3', header: this.l('OrgLevel3'), sortable: false, width: 160 },
        { field: 'orgLevel4', header: this.l('OrgLevel4'), sortable: false, width: 160 },
        { field: 'orgLevel5', header: this.l('OrgLevel5'), sortable: false, width: 160 },
        { field: 'orgLevel6', header: this.l('OrgLevel6'), sortable: false, width: 160 },
        { field: 'orgLevel7', header: this.l('OrgLevel7'), sortable: false, width: 160 },
        { field: 'date', header: this.l('Date'), sortable: true, width: 130, isDate: true },
        { field: 'weekDay', header: this.l('WeekDay'), sortable: false, width: 130 },
        { field: 'shiftStart', header: this.l('ShiftStart'), sortable: false, width: 130 },
        { field: 'shiftEnd', header: this.l('ShiftEnd'), sortable: false, width: 130 },
        { field: 'signIn', header: this.l('SignIn'), sortable: false, width: 120 },
        { field: 'signOut', header: this.l('SignOut'), sortable: false, width: 120 },
        { field: 'breakOut', header: this.l('BreakOut'), sortable: false, width: 120 },
        { field: 'breakIn', header: this.l('BreakIn'), sortable: false, width: 120 },
        { field: 'breakHours', header: this.l('BreakHours'), sortable: false, width: 140 },
        { field: 'earlyIn', header: this.l('EarlyIn'), sortable: false, width: 120 },
        { field: 'earlyOut', header: this.l('EarlyOut'), sortable: false, width: 120 },
        { field: 'lateIn', header: this.l('LateIn'), sortable: false, width: 120 },
        { field: 'lateOut', header: this.l('LateOut'), sortable: false, width: 120 },
        { field: 'totalHours', header: this.l('TotalHours'), sortable: false, width: 140 },
        { field: 'plannedHours', header: this.l('PlannedHours'), sortable: false, width: 150 },
        { field: 'overtimeHours', header: this.l('OvertimeHours'), sortable: false, width: 150 },
        { field: 'absenceHours', header: this.l('AbsenceHours'), sortable: false, width: 150 },
        { field: 'status', header: this.l('Status'), sortable: false, width: 130 },
        { field: 'leaveType', header: this.l('LeaveType'), sortable: false, width: 170 },
        { field: 'partialLeaveHours', header: this.l('PartialLeaveHours'), sortable: false, width: 180 },
        { field: 'punchInAccessUnit', header: this.l('PunchInAccessUnit'), sortable: false, width: 190 },
        { field: 'punchOutAccessUnit', header: this.l('PunchOutAccessUnit'), sortable: false, width: 200 },
        { field: 'timePolicyName', header: this.l('TimePolicy'), sortable: false, width: 170 },
        { field: 'workLocationName', header: this.l('WorkLocation'), sortable: false, width: 170 },
        { field: 'countryName', header: this.l('Country'), sortable: false, width: 150 },
        { field: 'punchInLocation', header: this.l('PunchInLocation'), sortable: false, width: 180 },
        { field: 'punchOutLocation', header: this.l('PunchOutLocation'), sortable: false, width: 190 },
    ];

    /**
     * Loads ONLY the report-specific lookups. The grid itself is still not fetched here -- the table's
     * (onLazyLoad) fires the first getRecords().
     */
    ngOnInit(): void {
        this._payrollAreaService
            .getAll(
                new GetAllPayrollAreasInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.payrollAreaCodeOptions.set(
                    result.items.map((item) => ({
                        label: item.name ?? item.code ?? '',
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
            .getLateEarlyLogReport(
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
            .getLateEarlyLogReportToExcel(
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

    private buildInput(sorting: string, maxResultCount: number, skipCount: number): GetLateEarlyLogReportInput {
        return new GetLateEarlyLogReportInput({
            fromDate: this.fromDate() ? DateTime.fromJSDate(this.fromDate()) : undefined,
            toDate: this.toDate() ? DateTime.fromJSDate(this.toDate()) : undefined,
            employeeCode: this.employeeCode(),
            employeeStatus: this.employeeStatus(),
            payrollAreaCode: this.payrollAreaCode(),
            sorting: sorting,
            maxResultCount: maxResultCount,
            skipCount: skipCount,
        });
    }
}
