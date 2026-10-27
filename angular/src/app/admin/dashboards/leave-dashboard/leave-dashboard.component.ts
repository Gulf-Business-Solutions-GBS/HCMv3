import { Component, ElementRef, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BarChartModule, Color, LegendPosition, PieChartModule, ScaleType } from '@swimlane/ngx-charts';
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
    DashboardScopeReason,
    ExportLeaveDashboardInput,
    GetLeaveDashboardLeavesInput,
    LeaveDashboardAbsenceTypeOptionDto,
    LeaveDashboardDto,
    LeaveDashboardExportPart,
    LeaveDashboardGranularity,
    LeaveDashboardInput,
    LeaveDashboardLeaveRowDto,
    LeaveDashboardOrgUnitBucketDto,
    LeaveDashboardServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { CountUpDirective } from '../shared/count-up.directive';
import { DashboardExportMenuComponent } from '../shared/dashboard-export-menu.component';
import { PERIOD_PRESETS, PeriodPreset, periodOf, presetLabelKey, scopeLabel } from '../shared/dashboard-shared';

/** Which list the detail panel is showing. */
type DetailKind = 'onLeaveToday' | 'notReturned' | 'startingSoon' | 'returningSoon' | 'pending' | 'period' | 'day';

interface ChartItem {
    name: string;
    value: number;
}

/**
 * Leave Dashboard: replaces the legacy HR "Leave Dashboard" (on leave today / not returned) and the ESS Leave
 * Planner charts.
 *
 * Drill-down never leaves the page. Clicking a department or a leave type filters the whole dashboard (and
 * shows as a removable chip); clicking a month narrows the period to that month, which switches the trend to
 * days; clicking a day lists who was out that day.
 *
 * Whose leave is shown is never a choice on this page: the server works it out from the signed-in user
 * (IDashboardScopeService -- row-level security, the Admin role, their own team) and says so in the toolbar.
 */
@Component({
    templateUrl: './leave-dashboard.component.html',
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
        PieChartModule,
        Popover,
        CountUpDirective,
        DashboardExportMenuComponent,
        HelpPageDirective,
    ],
})
export class LeaveDashboardComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(LeaveDashboardServiceProxy);
    private readonly _router = inject(Router);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly paginator = viewChild<Paginator>('paginator');
    readonly detailPanel = viewChild<ElementRef<HTMLElement>>('detailPanel');
    readonly filterPanel = viewChild<Popover>('filterPanel');

    readonly Part = LeaveDashboardExportPart;
    readonly presets = PERIOD_PRESETS;

    readonly isArabic = abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    readonly legendBelow = LegendPosition.Below;

    // ------------------------------------------------------------ filters

    readonly absenceTypes = signal<LeaveDashboardAbsenceTypeOptionDto[]>([]);
    readonly optionsLoaded = signal(false);

    // Applied filters: what the dashboard currently shows.
    readonly preset = signal<PeriodPreset>('yearToDate');
    readonly fromDate = signal<Date>(periodOf('yearToDate').from);
    readonly toDate = signal<Date>(periodOf('yearToDate').to);
    readonly organizationUnitId = signal<number | undefined>(undefined);
    readonly absenceTypeId = signal<number | undefined>(undefined);

    // Draft filters: what the filter window is editing. Copied to the applied ones only on Apply.
    readonly draftPreset = signal<PeriodPreset>('yearToDate');
    readonly draftFrom = signal<Date>(new Date());
    readonly draftTo = signal<Date>(new Date());
    readonly draftUnitId = signal<number | undefined>(undefined);
    readonly draftTypeId = signal<number | undefined>(undefined);

    readonly exporting = signal(false);

    /** The period before a month was clicked, so removing the month chip goes back to it. */
    readonly periodBeforeDrill = signal<{ from: Date; to: Date; preset: PeriodPreset } | undefined>(undefined);

    // ------------------------------------------------------------ data

    readonly dashboard = signal<LeaveDashboardDto | undefined>(undefined);
    readonly loading = signal(false);

    readonly detailKind = signal<DetailKind | undefined>(undefined);
    readonly detailDay = signal<DateTime | undefined>(undefined);
    readonly serverRows = signal<LeaveDashboardLeaveRowDto[]>([]);
    readonly serverTotal = signal(0);
    readonly serverLoading = signal(false);

    readonly selectedTypeName = computed(() => {
        const id = this.absenceTypeId();
        const type = this.absenceTypes().find((t) => t.id === id);
        return type ? this.pick(type.nameEn, type.nameAr) : '';
    });

    /** Stable colour per leave type, shared by the stacked bars and the donut. */
    private readonly palette = [
        '#3b82f6',
        '#f59e0b',
        '#10b981',
        '#ef4444',
        '#8b5cf6',
        '#06b6d4',
        '#f97316',
        '#84cc16',
        '#ec4899',
        '#64748b',
    ];

    readonly typeColors = computed(() =>
        this.absenceTypes().map((t, i) => ({
            name: this.pick(t.nameEn, t.nameAr),
            value: this.palette[i % this.palette.length],
        }))
    );

    readonly singleColour: Color = {
        name: 'leaveDashboardSingle',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#3b82f6'],
    };

    readonly trendIsDaily = computed(() => this.dashboard()?.granularity === LeaveDashboardGranularity.Day);

    /** ngx-charts stacked series: one group per bucket, one bar segment per leave type. */
    readonly trendResults = computed(() => {
        const dto = this.dashboard();
        if (!dto) {
            return [];
        }
        const daily = dto.granularity === LeaveDashboardGranularity.Day;
        return dto.trend.map((point) => ({
            name: this.trendLabel(point.periodStart, daily),
            series: point.values.map((v) => ({ name: this.typeName(v.absenceTypeId), value: v.value })),
        }));
    });

    readonly trendHasData = computed(() => this.trendResults().some((g) => g.series.length > 0));

    readonly orgUnitResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.byOrgUnit ?? []).map((b) => ({ name: this.orgUnitLabel(b), value: b.leaveDays }))
    );

    readonly typeResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.byType ?? []).map((b) => ({ name: this.pick(b.nameEn, b.nameAr), value: b.leaveDays }))
    );

    readonly detailRows = computed<LeaveDashboardLeaveRowDto[]>(() => {
        const dto = this.dashboard();
        switch (this.detailKind()) {
            case 'onLeaveToday':
                return dto?.onLeaveToday ?? [];
            case 'notReturned':
                return dto?.notReturned ?? [];
            case 'startingSoon':
                return dto?.startingSoon ?? [];
            case 'returningSoon':
                return dto?.returningSoon ?? [];
            case 'pending':
                return dto?.pendingApproval ?? [];
            default:
                return [];
        }
    });

    readonly detailIsServer = computed(() => this.detailKind() === 'period' || this.detailKind() === 'day');

    readonly detailTitle = computed(() => {
        switch (this.detailKind()) {
            case 'onLeaveToday':
                return this.l('LeaveDashboard_OnLeaveToday');
            case 'notReturned':
                return this.l('LeaveDashboard_NotReturned');
            case 'startingSoon':
                return this.l('LeaveDashboard_StartingSoon');
            case 'returningSoon':
                return this.l('LeaveDashboard_ReturningSoon');
            case 'pending':
                return this.l('LeaveDashboard_PendingApproval');
            case 'period':
                return this.l('LeaveDashboard_LeaveInPeriod');
            case 'day':
                return this.l('LeaveDashboard_OutOnDay', this.detailDay()?.toFormat('dd/MM/yyyy'));
            default:
                return '';
        }
    });

    ngOnInit(): void {
        this._service.getOptions().subscribe((options) => {
            this.absenceTypes.set(options.absenceTypes ?? []);
            this.optionsLoaded.set(true);
            this.refresh();
        });
    }

    // ------------------------------------------------------------ loading

    /**
     * Reloads every tile. The tiles stay on screen and clickable meanwhile, so a second click before the first
     * answer arrives cancels the first request -- otherwise a slow older answer could land last and win.
     */
    refresh(): void {
        this._dashboardRequest?.unsubscribe();
        this.loading.set(true);

        const request = this._service
            .getDashboard(this.buildInput())
            .pipe(
                finalize(() => {
                    if (this._dashboardRequest === request) {
                        this.loading.set(false);
                    }
                })
            )
            .subscribe((dto) => {
                this.dashboard.set(dto);
                if (this.detailIsServer()) {
                    this.reloadServerRows();
                }
            });
        this._dashboardRequest = request;
    }

    private _dashboardRequest: Subscription | undefined;

    /** First page comes from the paginator's own (onPageChange) wiring below; this reloads the current one. */
    getServerRows(event?: LazyLoadEvent): void {
        const rows = event?.rows ?? this.paginator()?.rows ?? 10;
        const first = event?.first ?? 0;

        const filters = this.buildInput();
        const input = new GetLeaveDashboardLeavesInput();
        input.fromDate = filters.fromDate;
        input.toDate = filters.toDate;
        input.organizationUnitId = filters.organizationUnitId;
        input.absenceTypeId = filters.absenceTypeId;
        input.day = this.detailKind() === 'day' ? this.detailDay() : undefined;
        input.skipCount = first;
        input.maxResultCount = rows;

        this.serverLoading.set(true);
        this._service
            .getLeaves(input)
            .pipe(finalize(() => this.serverLoading.set(false)))
            .subscribe((result) => {
                this.serverTotal.set(result.totalCount);
                this.serverRows.set(result.items);
            });
    }

    private reloadServerRows(): void {
        this.paginator()?.changePage(0);
        this.getServerRows({ first: 0, rows: this.paginator()?.rows ?? 10 });
    }

    private buildInput(): LeaveDashboardInput {
        const input = new LeaveDashboardInput();
        input.fromDate = DateTime.fromJSDate(this.fromDate()).startOf('day');
        input.toDate = DateTime.fromJSDate(this.toDate()).startOf('day');
        input.organizationUnitId = this.organizationUnitId();
        input.absenceTypeId = this.absenceTypeId();
        return input;
    }

    // ------------------------------------------------------------ filter actions

    presetLabel(preset: PeriodPreset): string {
        return this.l(presetLabelKey(preset));
    }

    /** Opens the filter window with a copy of what is applied, so closing it without Apply changes nothing. */
    openFilters(event: Event): void {
        this.draftPreset.set(this.periodBeforeDrill() ? 'custom' : this.preset());
        this.draftFrom.set(this.fromDate());
        this.draftTo.set(this.toDate());
        this.draftUnitId.set(this.organizationUnitId());
        this.draftTypeId.set(this.absenceTypeId());
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
        this.absenceTypeId.set(this.draftTypeId());
        this.periodBeforeDrill.set(undefined);

        this.filterPanel()?.hide();
        this.refresh();
    }

    resetFilters(): void {
        this.pickPreset('yearToDate');
        this.draftUnitId.set(undefined);
        this.draftTypeId.set(undefined);
    }

    // ------------------------------------------------------------ export

    /** The whole dashboard, or one tile. Uses the applied filters, so the file matches the screen. */
    exportToExcel(part: LeaveDashboardExportPart, day?: DateTime): void {
        const filters = this.buildInput();
        const input = new ExportLeaveDashboardInput();
        input.fromDate = filters.fromDate;
        input.toDate = filters.toDate;
        input.organizationUnitId = filters.organizationUnitId;
        input.absenceTypeId = filters.absenceTypeId;
        input.part = part;
        input.day = day;

        this.exporting.set(true);
        this._service
            .exportToExcel(input)
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    /** The detail panel exports whichever list it is showing. */
    exportDetail(): void {
        switch (this.detailKind()) {
            case 'onLeaveToday':
                return this.exportToExcel(LeaveDashboardExportPart.OnLeaveToday);
            case 'notReturned':
                return this.exportToExcel(LeaveDashboardExportPart.NotReturned);
            case 'startingSoon':
                return this.exportToExcel(LeaveDashboardExportPart.StartingSoon);
            case 'returningSoon':
                return this.exportToExcel(LeaveDashboardExportPart.ReturningSoon);
            case 'pending':
                return this.exportToExcel(LeaveDashboardExportPart.PendingApproval);
            case 'day':
                return this.exportToExcel(LeaveDashboardExportPart.LeavesInPeriod, this.detailDay());
            default:
                return this.exportToExcel(LeaveDashboardExportPart.LeavesInPeriod);
        }
    }

    selectOrgUnit(id: number | undefined): void {
        this.organizationUnitId.set(id ?? undefined);
        this.refresh();
    }

    selectAbsenceType(id: number | undefined): void {
        this.absenceTypeId.set(id ?? undefined);
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

    clearAll(): void {
        this.organizationUnitId.set(undefined);
        this.absenceTypeId.set(undefined);
        if (this.periodBeforeDrill()) {
            this.clearMonthDrill();
        } else {
            this.refresh();
        }
    }

    // ------------------------------------------------------------ chart drill-down

    onTrendSelect(event: { series?: string; name?: string }): void {
        const dto = this.dashboard();
        const label = event.series ?? event.name;
        if (!dto || !label) {
            return;
        }

        const daily = dto.granularity === LeaveDashboardGranularity.Day;
        const point = dto.trend.find((p) => this.trendLabel(p.periodStart, daily) === label);
        if (!point) {
            return;
        }

        if (daily) {
            this.detailDay.set(point.periodStart);
            this.showDetail('day');
            return;
        }

        // Month: narrow the period to that month; the trend then shows its days.
        if (!this.periodBeforeDrill()) {
            this.periodBeforeDrill.set({ from: this.fromDate(), to: this.toDate(), preset: this.preset() });
        }
        const month = point.periodStart.toJSDate();
        this.preset.set('custom');
        this.fromDate.set(new Date(month.getFullYear(), month.getMonth(), 1));
        this.toDate.set(new Date(month.getFullYear(), month.getMonth() + 1, 0));
        this.refresh();
    }

    onOrgUnitSelect(event: { name?: string }): void {
        const bucket = (this.dashboard()?.byOrgUnit ?? []).find((b) => this.orgUnitLabel(b) === event.name);
        // Direct staff and the "unassigned" bar have nothing below them to open.
        if (!bucket || bucket.isDirectStaff || bucket.organizationUnitId == null) {
            return;
        }
        this.selectOrgUnit(bucket.organizationUnitId);
    }

    onTypeSelect(event: { name?: string }): void {
        const bucket = (this.dashboard()?.byType ?? []).find((b) => this.pick(b.nameEn, b.nameAr) === event.name);
        if (bucket) {
            this.selectAbsenceType(bucket.absenceTypeId);
        }
    }

    // ------------------------------------------------------------ detail panel

    showDetail(kind: DetailKind): void {
        const wasServer = this.detailIsServer();
        this.detailKind.set(kind);

        // Switching between two server lists keeps the paginator mounted, so it will not fire again by itself.
        if (wasServer && this.detailIsServer()) {
            this.reloadServerRows();
        } else if (this.detailIsServer()) {
            this.getServerRows({ first: 0, rows: 10 });
        }

        setTimeout(() => this.detailPanel()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }

    closeDetail(): void {
        this.detailKind.set(undefined);
    }

    openEmployee(row: LeaveDashboardLeaveRowDto): void {
        if (!this.permission.isGranted('Pages.PersonnelAdministration.Employees')) {
            return;
        }
        this._router.navigate(['/app/admin/employees', row.employeeId]);
    }

    // ------------------------------------------------------------ display helpers

    pick(en?: string, ar?: string): string {
        return (this.isArabic ? ar || en : en || ar) ?? '';
    }

    employeeName(row: LeaveDashboardLeaveRowDto): string {
        return this.pick(row.employeeNameEn, row.employeeNameAr);
    }

    typeNameOf(row: LeaveDashboardLeaveRowDto): string {
        return this.pick(row.absenceTypeNameEn, row.absenceTypeNameAr);
    }

    /** "All employees (Admin)", "Finance + Payroll + your team", ... -- shared wording, see dashboard-shared.ts. */
    readonly scopeLabel = computed(() => scopeLabel(this.dashboard()?.scope, (key) => this.l(key)));

    readonly scopeIsNobody = computed(() => this.dashboard()?.scope?.reason === DashboardScopeReason.Nobody);

    indent(depth: number): string {
        return '   '.repeat(depth);
    }

    private typeName(absenceTypeId: number): string {
        const type = this.absenceTypes().find((t) => t.id === absenceTypeId);
        return type ? this.pick(type.nameEn, type.nameAr) : '#' + absenceTypeId;
    }

    private trendLabel(date: DateTime, daily: boolean): string {
        return daily ? date.toFormat('dd MMM') : date.toFormat('MMM yyyy');
    }

    private orgUnitLabel(bucket: LeaveDashboardOrgUnitBucketDto): string {
        if (bucket.organizationUnitId == null) {
            return this.l('LeaveDashboard_Unassigned');
        }
        const name = bucket.name ?? '#' + bucket.organizationUnitId;
        return bucket.isDirectStaff ? this.l('LeaveDashboard_DirectStaff', name) : name;
    }
}
