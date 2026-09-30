import { Component, ElementRef, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BarChartModule, Color, LegendPosition, ScaleType } from '@swimlane/ngx-charts';
import { DateTime } from 'luxon';
import { LazyLoadEvent } from 'primeng/api';
import { DatePicker } from 'primeng/datepicker';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Popover } from 'primeng/popover';
import { TableModule } from 'primeng/table';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    AttendanceDashboardDto,
    AttendanceDashboardExportPart,
    AttendanceDashboardGranularity,
    AttendanceDashboardInput,
    AttendanceDashboardServiceProxy,
    AttendanceExceptionKind,
    AttendanceExceptionRowDto,
    AttendanceOrgUnitBucketDto,
    AttendancePunchRequestRowDto,
    AttendanceTopEmployeeDto,
    DashboardScopeReason,
    ExportAttendanceDashboardInput,
    GetAttendanceExceptionsInput,
    GetAttendancePunchRequestsInput,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { CountUpDirective } from '../shared/count-up.directive';
import { DashboardExportMenuComponent } from '../shared/dashboard-export-menu.component';
import { PERIOD_PRESETS, PeriodPreset, periodOf, presetLabelKey, scopeLabel } from '../shared/dashboard-shared';

interface ChartItem {
    name: string;
    value: number;
}

/** What the list under the tiles is showing: attendance exceptions of a kind (optionally one day), or punch requests. */
type DetailList = { kind: 'exceptions'; exceptionKind: AttendanceExceptionKind; day?: DateTime } | { kind: 'punches' };

/**
 * Time & Attendance Dashboard: replaces the legacy ESS TimeAttendance / RawTimeAttendance dashboards. Built like the
 * Leave and Workforce dashboards from the same shared pieces. Reads the Time Engine's results, so an unevaluated
 * period shows the "run the Time Engine" note rather than zeros that look like perfect attendance.
 *
 * Clicking a month narrows the period to it; a day lists its exceptions; a department drills into its units; an
 * employee on a "most" chart filters every tile to that employee.
 */
@Component({
    templateUrl: './attendance-dashboard.component.html',
    styleUrls: ['../shared/dashboard.less'],
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        LocalizePipe,
        LuxonFormatPipe,
        DecimalPipe,
        DatePicker,
        TableModule,
        PaginatorModule,
        BarChartModule,
        Popover,
        CountUpDirective,
        DashboardExportMenuComponent,
        HelpPageDirective,
    ],
})
export class AttendanceDashboardComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(AttendanceDashboardServiceProxy);
    private readonly _router = inject(Router);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly paginator = viewChild<Paginator>('paginator');
    readonly detailPanel = viewChild<ElementRef<HTMLElement>>('detailPanel');
    readonly filterPanel = viewChild<Popover>('filterPanel');

    readonly Part = AttendanceDashboardExportPart;
    readonly Kind = AttendanceExceptionKind;
    readonly presets = PERIOD_PRESETS;
    readonly isArabic = abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    readonly legendBelow = LegendPosition.Below;

    // ------------------------------------------------------------ filters

    readonly preset = signal<PeriodPreset>('thisMonth');
    readonly fromDate = signal<Date>(periodOf('thisMonth').from);
    readonly toDate = signal<Date>(periodOf('thisMonth').to);
    readonly organizationUnitId = signal<number | undefined>(undefined);
    readonly employeeId = signal<number | undefined>(undefined);

    /** The period before a month was clicked, so removing the month chip goes back to it. */
    readonly periodBeforeDrill = signal<{ from: Date; to: Date; preset: PeriodPreset } | undefined>(undefined);

    readonly draftPreset = signal<PeriodPreset>('thisMonth');
    readonly draftFrom = signal<Date>(new Date());
    readonly draftTo = signal<Date>(new Date());
    readonly draftUnitId = signal<number | undefined>(undefined);

    // ------------------------------------------------------------ data

    readonly dashboard = signal<AttendanceDashboardDto | undefined>(undefined);
    readonly loading = signal(false);
    readonly exporting = signal(false);

    readonly detail = signal<DetailList | undefined>(undefined);
    readonly exceptionRows = signal<AttendanceExceptionRowDto[]>([]);
    readonly punchRows = signal<AttendancePunchRequestRowDto[]>([]);
    readonly rowsTotal = signal(0);
    readonly rowsLoading = signal(false);

    private _dashboardRequest: Subscription | undefined;

    // ------------------------------------------------------------ chart data

    readonly singleColour: Color = { name: 'taSingle', selectable: true, group: ScaleType.Ordinal, domain: ['#10b981'] };
    readonly redColour: Color = { name: 'taRed', selectable: true, group: ScaleType.Ordinal, domain: ['#ef4444'] };
    readonly amberColour: Color = { name: 'taAmber', selectable: true, group: ScaleType.Ordinal, domain: ['#f59e0b'] };
    readonly violetColour: Color = { name: 'taViolet', selectable: true, group: ScaleType.Ordinal, domain: ['#8b5cf6'] };
    readonly trendColour: Color = {
        name: 'taTrend',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#ef4444', '#f59e0b', '#8b5cf6'],
    };
    readonly weekdayColour: Color = { name: 'taWeekday', selectable: true, group: ScaleType.Ordinal, domain: ['#f59e0b', '#ef4444'] };

    readonly trendIsDaily = computed(() => this.dashboard()?.granularity === AttendanceDashboardGranularity.Day);

    readonly trendResults = computed(() => {
        const d = this.dashboard();
        if (!d) {
            return [];
        }
        const daily = d.granularity === AttendanceDashboardGranularity.Day;
        return d.trend.map((p) => ({
            name: this.trendLabel(p.periodStart, daily),
            series: [
                { name: this.l('AttendanceDashboard_AbsentDays'), value: p.absentDays },
                { name: this.l('AttendanceDashboard_LateArrivals'), value: p.lateDays },
                { name: this.l('AttendanceDashboard_EarlyDepartures'), value: p.earlyDays },
            ],
        }));
    });

    readonly hasData = computed(() => (this.dashboard()?.kpis.workingDays ?? 0) > 0);

    readonly orgUnitResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.byOrgUnit ?? []).map((b) => ({ name: this.orgUnitLabel(b), value: b.attendanceRatePercent ?? 0 }))
    );

    readonly weekdayResults = computed(() =>
        (this.dashboard()?.weekdays ?? []).map((w) => ({
            name: this.weekdayName(w.dayOfWeek),
            series: [
                { name: this.l('AttendanceDashboard_LateArrivals'), value: w.lateDays },
                { name: this.l('AttendanceDashboard_AbsentDays'), value: w.absentDays },
            ],
        }))
    );

    readonly topAbsentResults = computed<ChartItem[]>(() => this.top(this.dashboard()?.topAbsent));
    readonly topLateResults = computed<ChartItem[]>(() => this.top(this.dashboard()?.topLate));
    readonly topEarlyResults = computed<ChartItem[]>(() => this.top(this.dashboard()?.topEarly));

    readonly scopeLabel = computed(() => scopeLabel(this.dashboard()?.scope, (key) => this.l(key)));
    readonly scopeIsNobody = computed(() => this.dashboard()?.scope?.reason === DashboardScopeReason.Nobody);

    readonly employeeName = computed(() => {
        const d = this.dashboard();
        return d ? this.pick(d.employeeNameEn, d.employeeNameAr) : '';
    });

    readonly detailTitle = computed(() => {
        const list = this.detail();
        if (!list) {
            return '';
        }
        if (list.kind === 'punches') {
            return this.l('AttendanceDashboard_PunchRequests');
        }
        if (list.day) {
            return this.l('AttendanceDashboard_ExceptionsOnDay', list.day.toFormat('dd/MM/yyyy'));
        }
        switch (list.exceptionKind) {
            case AttendanceExceptionKind.Absent:
                return this.l('AttendanceDashboard_AbsentDays');
            case AttendanceExceptionKind.Late:
                return this.l('AttendanceDashboard_LateArrivals');
            case AttendanceExceptionKind.EarlyOut:
                return this.l('AttendanceDashboard_EarlyDepartures');
            case AttendanceExceptionKind.MissingPunch:
                return this.l('AttendanceDashboard_MissingPunches');
            default:
                return this.l('AttendanceDashboard_Exceptions');
        }
    });

    readonly detailIsPunches = computed(() => this.detail()?.kind === 'punches');

    ngOnInit(): void {
        this.refresh();
    }

    // ------------------------------------------------------------ loading

    /** Reloads every tile in place; a newer request cancels an older one so the latest click always wins. */
    refresh(): void {
        this._dashboardRequest?.unsubscribe();
        this.loading.set(true);

        const request = this._service
            .getDashboard(this.buildInput(new AttendanceDashboardInput()))
            .pipe(
                finalize(() => {
                    if (this._dashboardRequest === request) {
                        this.loading.set(false);
                    }
                })
            )
            .subscribe((dto) => {
                this.dashboard.set(dto);
                if (this.detail()) {
                    this.reloadRows();
                }
            });
        this._dashboardRequest = request;
    }

    getRows(event?: LazyLoadEvent): void {
        const list = this.detail();
        if (!list) {
            return;
        }

        const skip = event?.first ?? 0;
        const max = event?.rows ?? this.paginator()?.rows ?? 10;
        this.rowsLoading.set(true);

        if (list.kind === 'punches') {
            const input = this.buildInput(new GetAttendancePunchRequestsInput());
            input.skipCount = skip;
            input.maxResultCount = max;
            this._service
                .getPunchRequests(input)
                .pipe(finalize(() => this.rowsLoading.set(false)))
                .subscribe((result) => {
                    this.rowsTotal.set(result.totalCount);
                    this.punchRows.set(result.items);
                });
            return;
        }

        const input = this.buildInput(new GetAttendanceExceptionsInput());
        input.kind = list.exceptionKind;
        input.day = list.day;
        input.skipCount = skip;
        input.maxResultCount = max;
        this._service
            .getExceptions(input)
            .pipe(finalize(() => this.rowsLoading.set(false)))
            .subscribe((result) => {
                this.rowsTotal.set(result.totalCount);
                this.exceptionRows.set(result.items);
            });
    }

    private reloadRows(): void {
        this.paginator()?.changePage(0);
        this.getRows({ first: 0, rows: this.paginator()?.rows ?? 10 });
    }

    private buildInput<T extends AttendanceDashboardInput>(input: T): T {
        input.fromDate = DateTime.fromJSDate(this.fromDate()).startOf('day');
        input.toDate = DateTime.fromJSDate(this.toDate()).startOf('day');
        input.organizationUnitId = this.organizationUnitId();
        input.employeeId = this.employeeId();
        return input;
    }

    // ------------------------------------------------------------ filter window

    presetLabel(preset: PeriodPreset): string {
        return this.l(presetLabelKey(preset));
    }

    openFilters(event: Event): void {
        this.draftPreset.set(this.periodBeforeDrill() ? 'custom' : this.preset());
        this.draftFrom.set(this.fromDate());
        this.draftTo.set(this.toDate());
        this.draftUnitId.set(this.organizationUnitId());
        this.filterPanel()?.toggle(event);
    }

    pickPreset(preset: PeriodPreset): void {
        this.draftPreset.set(preset);
        if (preset !== 'custom') {
            const period = periodOf(preset);
            this.draftFrom.set(period.from);
            this.draftTo.set(period.to);
        }
    }

    applyFilters(): void {
        this.preset.set(this.draftPreset());
        this.fromDate.set(this.draftFrom());
        this.toDate.set(this.draftTo());
        this.organizationUnitId.set(this.draftUnitId());
        this.periodBeforeDrill.set(undefined);
        this.filterPanel()?.hide();
        this.refresh();
    }

    resetFilters(): void {
        this.pickPreset('thisMonth');
        this.draftUnitId.set(undefined);
    }

    // ------------------------------------------------------------ drill-down

    onTrendSelect(event: { series?: string; name?: string }): void {
        const d = this.dashboard();
        const label = event.series ?? event.name;
        if (!d || !label) {
            return;
        }

        const daily = d.granularity === AttendanceDashboardGranularity.Day;
        const point = d.trend.find((p) => this.trendLabel(p.periodStart, daily) === label);
        if (!point) {
            return;
        }

        if (daily) {
            this.showList({ kind: 'exceptions', exceptionKind: AttendanceExceptionKind.Any, day: point.periodStart });
            return;
        }

        if (!this.periodBeforeDrill()) {
            this.periodBeforeDrill.set({ from: this.fromDate(), to: this.toDate(), preset: this.preset() });
        }
        const month = point.periodStart.toJSDate();
        this.preset.set('custom');
        this.fromDate.set(new Date(month.getFullYear(), month.getMonth(), 1));
        this.toDate.set(new Date(month.getFullYear(), month.getMonth() + 1, 0));
        this.refresh();
    }

    clearMonthDrill(): void {
        const before = this.periodBeforeDrill();
        if (before) {
            this.fromDate.set(before.from);
            this.toDate.set(before.to);
            this.preset.set(before.preset);
        }
        this.periodBeforeDrill.set(undefined);
        this.refresh();
    }

    selectOrgUnit(id: number | undefined): void {
        this.organizationUnitId.set(id ?? undefined);
        this.refresh();
    }

    onOrgUnitSelect(event: { name?: string }): void {
        const bucket = (this.dashboard()?.byOrgUnit ?? []).find((b) => this.orgUnitLabel(b) === event.name);
        if (bucket && !bucket.isDirectStaff && bucket.organizationUnitId != null) {
            this.selectOrgUnit(bucket.organizationUnitId);
        }
    }

    onEmployeeSelect(source: AttendanceTopEmployeeDto[] | undefined, event: { name?: string }): void {
        const employee = (source ?? []).find((t) => this.topLabel(t) === event.name);
        if (employee) {
            this.employeeId.set(employee.employeeId);
            this.refresh();
        }
    }

    clearEmployee(): void {
        this.employeeId.set(undefined);
        this.refresh();
    }

    clearAll(): void {
        this.organizationUnitId.set(undefined);
        this.employeeId.set(undefined);
        if (this.periodBeforeDrill()) {
            this.clearMonthDrill();
        } else {
            this.refresh();
        }
    }

    // ------------------------------------------------------------ lists and export

    showExceptions(kind: AttendanceExceptionKind): void {
        this.showList({ kind: 'exceptions', exceptionKind: kind });
    }

    showPunchRequests(): void {
        this.showList({ kind: 'punches' });
    }

    private showList(list: DetailList): void {
        const wasOpen = this.detail() !== undefined;
        this.detail.set(list);
        this.exceptionRows.set([]);
        this.punchRows.set([]);

        if (wasOpen) {
            this.reloadRows();
        } else {
            this.getRows({ first: 0, rows: 10 });
        }

        setTimeout(() => this.detailPanel()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }

    isListing(kind: AttendanceExceptionKind): boolean {
        const list = this.detail();
        return list?.kind === 'exceptions' && !list.day && list.exceptionKind === kind;
    }

    closeList(): void {
        this.detail.set(undefined);
    }

    exportToExcel(part: AttendanceDashboardExportPart): void {
        const input = this.buildInput(new ExportAttendanceDashboardInput());
        input.part = part;
        const list = this.detail();
        if (part === AttendanceDashboardExportPart.Exceptions && list?.kind === 'exceptions') {
            input.kind = list.exceptionKind;
            input.day = list.day;
        }

        this.exporting.set(true);
        this._service
            .exportToExcel(input)
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    exportList(): void {
        this.exportToExcel(
            this.detailIsPunches() ? AttendanceDashboardExportPart.PunchRequests : AttendanceDashboardExportPart.Exceptions
        );
    }

    openEmployee(employeeId: number): void {
        if (this.permission.isGranted('Pages.PersonnelAdministration.Employees')) {
            this._router.navigate(['/app/admin/employees', employeeId]);
        }
    }

    // ------------------------------------------------------------ labels

    pick(en?: string, ar?: string): string {
        return (this.isArabic ? ar || en : en || ar) ?? '';
    }

    exceptionLabel(r: AttendanceExceptionRowDto): string {
        const parts: string[] = [];
        if (r.isAbsent) parts.push(this.l('AttendanceDashboard_Absent'));
        if (r.lateMinutes > 0) parts.push(this.l('AttendanceDashboard_Late'));
        if (r.earlyMinutes > 0) parts.push(this.l('AttendanceDashboard_Early'));
        if (r.isMissingIn) parts.push(this.l('AttendanceDashboard_MissingIn'));
        if (r.isMissingOut) parts.push(this.l('AttendanceDashboard_MissingOut'));
        return parts.join(', ');
    }

    statusLabel(status: RequestStatus | undefined): string {
        return status === undefined || status === null ? '' : this.l(RequestStatus[status]);
    }

    orgUnitLabel(bucket: AttendanceOrgUnitBucketDto): string {
        if (bucket.organizationUnitId == null) {
            return this.l('LeaveDashboard_Unassigned');
        }
        const name = bucket.name ?? '#' + bucket.organizationUnitId;
        return bucket.isDirectStaff ? this.l('LeaveDashboard_DirectStaff', name) : name;
    }

    indent(depth: number): string {
        return '   '.repeat(depth);
    }

    topLabel(t: AttendanceTopEmployeeDto): string {
        return `${this.pick(t.employeeNameEn, t.employeeNameAr)} (${t.employeeCode})`;
    }

    private top(items: AttendanceTopEmployeeDto[] | undefined): ChartItem[] {
        return (items ?? []).map((t) => ({ name: this.topLabel(t), value: t.value }));
    }

    private weekdayName(dayOfWeek: number): string {
        // 4 Jan 2026 is a Sunday; add the day number to get a date on that weekday.
        return DateTime.fromObject({ year: 2026, month: 1, day: 4 + dayOfWeek }).toFormat('ccc');
    }

    private trendLabel(date: DateTime, daily: boolean): string {
        return daily ? date.toFormat('dd MMM') : date.toFormat('MMM yyyy');
    }
}
