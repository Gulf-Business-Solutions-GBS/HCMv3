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
    ExportOvertimeDashboardInput,
    GetOvertimeEntriesInput,
    OvertimeDashboardDto,
    OvertimeDashboardExportPart,
    OvertimeDashboardInput,
    OvertimeDashboardServiceProxy,
    OvertimeEntryListKind,
    OvertimeEntryRowDto,
    OvertimeOrgUnitBucketDto,
    OvertimeTopEmployeeDto,
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

/**
 * Overtime Dashboard: replaces the legacy ESS Overtime dashboard. Built like the other dashboards from the same shared
 * pieces. Hours are approved hours; cost is what payroll paid under each overtime type's wage type.
 *
 * Clicking a type filters every tile to it; a month lists its entries; a department drills into its units; a name
 * on "Most overtime" filters every tile to that employee.
 */
@Component({
    templateUrl: './overtime-dashboard.component.html',
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
export class OvertimeDashboardComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(OvertimeDashboardServiceProxy);
    private readonly _router = inject(Router);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly paginator = viewChild<Paginator>('paginator');
    readonly detailPanel = viewChild<ElementRef<HTMLElement>>('detailPanel');
    readonly filterPanel = viewChild<Popover>('filterPanel');

    readonly Part = OvertimeDashboardExportPart;
    readonly Kind = OvertimeEntryListKind;
    readonly presets = PERIOD_PRESETS;
    readonly isArabic = abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    readonly legendBelow = LegendPosition.Below;

    // ------------------------------------------------------------ filters

    readonly preset = signal<PeriodPreset>('yearToDate');
    readonly fromDate = signal<Date>(periodOf('yearToDate').from);
    readonly toDate = signal<Date>(periodOf('yearToDate').to);
    readonly organizationUnitId = signal<number | undefined>(undefined);
    readonly overtimeTypeId = signal<number | undefined>(undefined);
    readonly employeeId = signal<number | undefined>(undefined);

    readonly draftPreset = signal<PeriodPreset>('yearToDate');
    readonly draftFrom = signal<Date>(new Date());
    readonly draftTo = signal<Date>(new Date());
    readonly draftUnitId = signal<number | undefined>(undefined);

    // ------------------------------------------------------------ data

    readonly dashboard = signal<OvertimeDashboardDto | undefined>(undefined);
    readonly loading = signal(false);
    readonly exporting = signal(false);

    readonly detail = signal<{ kind: OvertimeEntryListKind; month?: DateTime } | undefined>(undefined);
    readonly rows = signal<OvertimeEntryRowDto[]>([]);
    readonly rowsTotal = signal(0);
    readonly rowsLoading = signal(false);

    private _dashboardRequest: Subscription | undefined;

    // ------------------------------------------------------------ chart data

    private readonly palette = ['#3b82f6', '#f59e0b', '#ef4444', '#8b5cf6', '#10b981', '#06b6d4', '#f97316', '#84cc16'];

    readonly singleColour: Color = {
        name: 'otSingle',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#3b82f6'],
    };
    readonly costColour: Color = { name: 'otCost', selectable: true, group: ScaleType.Ordinal, domain: ['#10b981'] };
    readonly topColour: Color = { name: 'otTop', selectable: true, group: ScaleType.Ordinal, domain: ['#f59e0b'] };

    /** Stable colour per overtime type, shared by the stacked bars and the donut. */
    readonly typeColours = computed(() =>
        (this.dashboard()?.types ?? []).map((t, i) => ({
            name: this.pick(t.nameEn, t.nameAr),
            value: this.palette[i % this.palette.length],
        }))
    );

    readonly trendResults = computed(() =>
        (this.dashboard()?.trend ?? []).map((p) => ({
            name: p.monthStart.toFormat('MMM yyyy'),
            series: p.hours.map((v) => ({ name: this.typeName(v.overtimeTypeId), value: v.hours })),
        }))
    );

    readonly costResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.trend ?? []).map((p) => ({ name: p.monthStart.toFormat('MMM yyyy'), value: p.cost }))
    );

    readonly hasHours = computed(() => (this.dashboard()?.kpis.approvedHours ?? 0) > 0);
    readonly hasCost = computed(() => (this.dashboard()?.kpis.cost ?? 0) > 0);

    readonly typeResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.byType ?? []).map((b) => ({ name: this.typeName(b.overtimeTypeId), value: b.hours }))
    );

    readonly orgUnitResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.byOrgUnit ?? []).map((b) => ({ name: this.orgUnitLabel(b), value: b.hours }))
    );

    readonly topResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.topEmployees ?? []).map((t) => ({ name: this.topLabel(t), value: t.hours }))
    );

    readonly scopeLabel = computed(() => scopeLabel(this.dashboard()?.scope, (key) => this.l(key)));
    readonly scopeIsNobody = computed(() => this.dashboard()?.scope?.reason === DashboardScopeReason.Nobody);

    readonly employeeName = computed(() => {
        const d = this.dashboard();
        return d ? this.pick(d.employeeNameEn, d.employeeNameAr) : '';
    });

    readonly selectedTypeName = computed(() => {
        const id = this.overtimeTypeId();
        return id === undefined ? '' : this.typeName(id);
    });

    readonly detailTitle = computed(() => {
        const list = this.detail();
        if (!list) {
            return '';
        }
        if (list.kind === OvertimeEntryListKind.Pending) {
            return this.l('OvertimeDashboard_PendingList');
        }
        return list.month
            ? this.l('OvertimeDashboard_EntriesInMonth', list.month.toFormat('MMMM yyyy'))
            : this.l('OvertimeDashboard_Entries');
    });

    ngOnInit(): void {
        this.refresh();
    }

    // ------------------------------------------------------------ loading

    /** Reloads every tile in place; a newer request cancels an older one so the latest click always wins. */
    refresh(): void {
        this._dashboardRequest?.unsubscribe();
        this.loading.set(true);

        const request = this._service
            .getDashboard(this.buildInput(new OvertimeDashboardInput()))
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

        const input = this.buildInput(new GetOvertimeEntriesInput());
        input.kind = list.kind;
        input.month = list.month;
        input.skipCount = event?.first ?? 0;
        input.maxResultCount = event?.rows ?? this.paginator()?.rows ?? 10;

        this.rowsLoading.set(true);
        this._service
            .getEntries(input)
            .pipe(finalize(() => this.rowsLoading.set(false)))
            .subscribe((result) => {
                this.rowsTotal.set(result.totalCount);
                this.rows.set(result.items);
            });
    }

    private reloadRows(): void {
        this.paginator()?.changePage(0);
        this.getRows({ first: 0, rows: this.paginator()?.rows ?? 10 });
    }

    private buildInput<T extends OvertimeDashboardInput>(input: T): T {
        input.fromDate = DateTime.fromJSDate(this.fromDate()).startOf('day');
        input.toDate = DateTime.fromJSDate(this.toDate()).startOf('day');
        input.organizationUnitId = this.organizationUnitId();
        input.overtimeTypeId = this.overtimeTypeId();
        input.employeeId = this.employeeId();
        return input;
    }

    // ------------------------------------------------------------ filter window

    presetLabel(preset: PeriodPreset): string {
        return this.l(presetLabelKey(preset));
    }

    openFilters(event: Event): void {
        this.draftPreset.set(this.preset());
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
        this.filterPanel()?.hide();
        this.refresh();
    }

    resetFilters(): void {
        this.pickPreset('yearToDate');
        this.draftUnitId.set(undefined);
    }

    // ------------------------------------------------------------ drill-down

    onTrendSelect(event: { series?: string; name?: string }): void {
        const label = event.series ?? event.name;
        const point = (this.dashboard()?.trend ?? []).find((p) => p.monthStart.toFormat('MMM yyyy') === label);
        if (point) {
            this.showList(OvertimeEntryListKind.Approved, point.monthStart);
        }
    }

    onTypeSelect(event: { name?: string }): void {
        const type = (this.dashboard()?.types ?? []).find((t) => this.pick(t.nameEn, t.nameAr) === event.name);
        if (type) {
            this.overtimeTypeId.set(type.id);
            this.refresh();
        }
    }

    clearType(): void {
        this.overtimeTypeId.set(undefined);
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

    onEmployeeSelect(event: { name?: string }): void {
        const employee = (this.dashboard()?.topEmployees ?? []).find((t) => this.topLabel(t) === event.name);
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
        this.overtimeTypeId.set(undefined);
        this.employeeId.set(undefined);
        this.refresh();
    }

    // ------------------------------------------------------------ lists and export

    showList(kind: OvertimeEntryListKind, month?: DateTime): void {
        const wasOpen = this.detail() !== undefined;
        this.detail.set({ kind, month });
        this.rows.set([]);

        if (wasOpen) {
            this.reloadRows();
        } else {
            this.getRows({ first: 0, rows: 10 });
        }

        setTimeout(() => this.detailPanel()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }

    isListing(kind: OvertimeEntryListKind): boolean {
        const list = this.detail();
        return list?.kind === kind && !list.month;
    }

    closeList(): void {
        this.detail.set(undefined);
    }

    exportToExcel(part: OvertimeDashboardExportPart): void {
        const input = this.buildInput(new ExportOvertimeDashboardInput());
        input.part = part;
        const list = this.detail();
        if (part === OvertimeDashboardExportPart.Entries && list) {
            input.kind = list.kind;
            input.month = list.month;
        }

        this.exporting.set(true);
        this._service
            .exportToExcel(input)
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
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

    typeName(id: number): string {
        const type = (this.dashboard()?.types ?? []).find((t) => t.id === id);
        return type ? this.pick(type.nameEn, type.nameAr) : '#' + id;
    }

    statusLabel(status: RequestStatus | undefined): string {
        // Entries keyed in directly carry no workflow status; they only reach the list once approved.
        return status === undefined || status === null ? this.l('Approved') : this.l(RequestStatus[status]);
    }

    orgUnitLabel(bucket: OvertimeOrgUnitBucketDto): string {
        if (bucket.organizationUnitId == null) {
            return this.l('LeaveDashboard_Unassigned');
        }
        const name = bucket.name ?? '#' + bucket.organizationUnitId;
        return bucket.isDirectStaff ? this.l('LeaveDashboard_DirectStaff', name) : name;
    }

    topLabel(t: OvertimeTopEmployeeDto): string {
        return `${this.pick(t.employeeNameEn, t.employeeNameAr)} (${t.employeeCode})`;
    }

    indent(depth: number): string {
        return '   '.repeat(depth);
    }
}
