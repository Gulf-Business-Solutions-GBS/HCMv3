import { Component, OnInit, inject, viewChild, signal, computed } from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';

import { Table, TableModule } from 'primeng/table';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { LazyLoadEvent } from 'primeng/api';
import { Tooltip } from 'primeng/tooltip';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { InputText } from 'primeng/inputtext';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    DutyPeriodDto,
    DutyPeriodsServiceProxy,
    GetAllDutyPeriodsInput,
    GetRosterDutyScheduleEmployeesInput,
    GetRosterDutyScheduleInput,
    RosterDutyScheduleDayDto,
    RosterDutyScheduleEmployeeDto,
    RosterDutyScheduleResourceDto,
    RosterScheduleViewerServiceProxy,
} from '@shared/service-proxies/service-proxies';

import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';

/** One rendered row of the schedule grid: a resource plus its resolved day cells, in column order. */
interface ScheduleRow {
    resource: RosterDutyScheduleResourceDto;
    cells: (RosterDutyScheduleDayDto | undefined)[];
}

/**
 * Port of the legacy WebForms screen
 * `HCM10.Web/UserControls/TimeManagement/uscRosterDutySchedule.ascx`.
 *
 * The legacy screen is READ-ONLY (see RosterScheduleViewerAppService remarks for the evidence: its only
 * write path was an in-Session BindingList, and its custom scheduler popup-menu items were never wired to
 * a click handler). This port is therefore a viewer with no create/edit/delete actions. CRUD over the
 * underlying entities lives on the existing dedicated screens (Duty Periods, Employee Duty Periods,
 * Roster Groups / Group Details / Group Schedules).
 *
 * The legacy DevExpress `ASPxScheduler` resource-grouped timeline is rendered here as a plain
 * employee x day matrix, which conveys the same information without pulling in a scheduler library.
 */
@Component({
    templateUrl: './roster-schedule-viewer.component.html',
    styleUrls: ['./roster-schedule-viewer.component.less'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        NgClass,
        TableModule,
        PaginatorModule,
        Tooltip,
        Select,
        DatePicker,
        InputText,
        LocalizePipe,
    ],
})
export class RosterScheduleViewerComponent extends AppComponentBase implements OnInit {
    private _viewerService = inject(RosterScheduleViewerServiceProxy);
    private _dutyPeriodsService = inject(DutyPeriodsServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** Pane 1 — duty period selector (legacy `dgvxRosterDutyPeriod`, rendered as a dropdown). */
    readonly dutyPeriods = signal<DutyPeriodDto[]>([]);
    selectedDutyPeriodId: number | undefined;

    asOfDate: Date = new Date();
    filter = '';

    /** Pane 2 — employees assigned to the selected duty period (legacy `dgvxEmployee`). */
    readonly selectedEmployeeIds = signal<number[]>([]);

    /** Pane 3 — the resolved schedule matrix (legacy `ASPxScheduler1`). */
    scheduleStartDate: Date = DateTime.now().startOf('month').toJSDate();
    scheduleEndDate: Date = DateTime.now().endOf('month').startOf('day').toJSDate();

    readonly scheduleLoading = signal(false);
    readonly scheduleColumns = signal<DateTime[]>([]);
    readonly scheduleRows = signal<ScheduleRow[]>([]);
    readonly scheduleLoaded = signal(false);

    readonly hasSelection = computed(() => this.selectedEmployeeIds().length > 0);

    ngOnInit(): void {
        // The employee table's (onLazyLoad) drives its own first load; only the duty period list is
        // fetched eagerly here because it populates the selector that gates everything else.
        this.loadDutyPeriods();
    }

    private loadDutyPeriods(): void {
        this._dutyPeriodsService
            .getAll(
                new GetAllDutyPeriodsInput({
                    filter: undefined,
                    sorting: 'NameEn ASC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .subscribe((result) => this.dutyPeriods.set(result.items ?? []));
    }

    /** Legacy `FillEmployeeGrid(dutyPeriod_Oid)`. */
    getEmployees(event?: LazyLoadEvent): void {
        if (!this.selectedDutyPeriodId) {
            this.primengTableHelper.totalRecordsCount.set(0);
            this.primengTableHelper.records.set([]);
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._viewerService
            .getRosterDutyScheduleEmployees(
                new GetRosterDutyScheduleEmployeesInput({
                    dutyPeriodId: this.selectedDutyPeriodId,
                    asOfDate: DateTime.fromJSDate(this.asOfDate).startOf('day'),
                    filter: this.filter || undefined,
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    onDutyPeriodChange(): void {
        this.selectedEmployeeIds.set([]);
        this.scheduleRows.set([]);
        this.scheduleColumns.set([]);
        this.scheduleLoaded.set(false);
        this.reloadEmployees();
    }

    reloadEmployees(): void {
        const rows = this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getEmployees({ first: 0, rows } as LazyLoadEvent);
    }

    isEmployeeSelected(record: RosterDutyScheduleEmployeeDto): boolean {
        return this.selectedEmployeeIds().includes(record.id);
    }

    toggleEmployee(record: RosterDutyScheduleEmployeeDto): void {
        const current = this.selectedEmployeeIds();
        this.selectedEmployeeIds.set(
            current.includes(record.id) ? current.filter((id) => id !== record.id) : [...current, record.id]
        );
    }

    selectAllOnPage(): void {
        const pageIds = (this.primengTableHelper.records() as RosterDutyScheduleEmployeeDto[]).map((r) => r.id);
        this.selectedEmployeeIds.set(Array.from(new Set([...this.selectedEmployeeIds(), ...pageIds])));
    }

    clearSelection(): void {
        this.selectedEmployeeIds.set([]);
        this.scheduleRows.set([]);
        this.scheduleColumns.set([]);
        this.scheduleLoaded.set(false);
    }

    /** Legacy `BindSchedular()`. */
    showSchedule(): void {
        if (!this.hasSelection()) {
            this.notify.warn(this.l('SelectEmployeesToViewSchedule'));
            return;
        }

        const start = DateTime.fromJSDate(this.scheduleStartDate).startOf('day');
        const end = DateTime.fromJSDate(this.scheduleEndDate).startOf('day');

        if (end < start) {
            this.notify.warn(this.l('EndDateCannotBeEarlierThanStartDate'));
            return;
        }

        this.scheduleLoading.set(true);

        this._viewerService
            .getRosterDutySchedule(
                new GetRosterDutyScheduleInput({
                    employeeIds: this.selectedEmployeeIds(),
                    startDate: start,
                    endDate: end,
                })
            )
            .pipe(finalize(() => this.scheduleLoading.set(false)))
            .subscribe((result) => {
                const columns: DateTime[] = [];
                for (let d = start; d <= end; d = d.plus({ days: 1 })) {
                    columns.push(d);
                }

                const byEmployee = new Map<number, Map<string, RosterDutyScheduleDayDto>>();
                for (const day of result.days ?? []) {
                    const key = DateTime.fromJSDate(new Date(day.dayDate as any)).toFormat('yyyy-MM-dd');
                    if (!byEmployee.has(day.employeeId)) {
                        byEmployee.set(day.employeeId, new Map());
                    }
                    byEmployee.get(day.employeeId)!.set(key, day);
                }

                this.scheduleColumns.set(columns);
                this.scheduleRows.set(
                    (result.resources ?? []).map((resource) => ({
                        resource,
                        cells: columns.map((c) => byEmployee.get(resource.id)?.get(c.toFormat('yyyy-MM-dd'))),
                    }))
                );
                this.scheduleLoaded.set(true);
            });
    }

    columnLabel(column: DateTime): string {
        return column.toFormat('dd');
    }

    columnTooltip(column: DateTime): string {
        return column.toFormat('yyyy-MM-dd (ccc)');
    }

    cellLabel(cell: RosterDutyScheduleDayDto | undefined): string {
        if (!cell || !cell.dutyName) {
            return '-';
        }
        return cell.dutyCode || cell.dutyName;
    }

    cellTooltip(cell: RosterDutyScheduleDayDto | undefined): string {
        if (!cell) {
            return this.l('NoDutyResolved');
        }
        if (cell.errorMessage) {
            return cell.errorMessage;
        }
        if (!cell.dutyName) {
            return this.l('NoDutyResolved');
        }
        // Legacy set the appointment Location to "<start> To <end>".
        return `${cell.dutyName} (${this.formatTime(cell.startTime)} - ${this.formatTime(cell.endTime)})`;
    }

    cellClass(cell: RosterDutyScheduleDayDto | undefined): Record<string, boolean> {
        return {
            'roster-cell': true,
            'roster-cell-empty': !cell || (!cell.dutyName && !cell.errorMessage),
            'roster-cell-error': !!cell?.errorMessage,
            'roster-cell-off': !!cell?.isWeeklyOff || !!cell?.isRestDay,
            'roster-cell-night': !!cell?.isNightShift,
            'roster-cell-half': !!cell?.isHalfDay,
        };
    }

    private formatTime(value: string | undefined): string {
        if (!value) {
            return '';
        }
        // NSwag maps TimeSpan to string ("HH:mm:ss"); show HH:mm.
        return value.length >= 5 ? value.substring(0, 5) : value;
    }
}
