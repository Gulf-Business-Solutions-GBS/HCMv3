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
    DashboardScopeReason,
    ExportWorkflowDashboardInput,
    GetWorkflowRequestsInput,
    RequestStatus,
    WorkflowApproverBucketDto,
    WorkflowDashboardDto,
    WorkflowDashboardExportPart,
    WorkflowDashboardInput,
    WorkflowDashboardServiceProxy,
    WorkflowOrgUnitBucketDto,
    WorkflowRequestListKind,
    WorkflowRequestRowDto,
    WorkflowWaitingBandDto,
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

/** What the request list is showing. */
interface RequestList {
    kind: WorkflowRequestListKind;
    status?: RequestStatus;
    month?: DateTime;
    approverId?: number;
    approverName?: string;
    band?: number;
}

/** The statuses the charts show, in order, each with its colour. Drafts never reach the dashboard. */
const STATUSES: { status: RequestStatus; colour: string }[] = [
    { status: RequestStatus.Open, colour: '#f59e0b' },
    { status: RequestStatus.Approved, colour: '#10b981' },
    { status: RequestStatus.Rejected, colour: '#ef4444' },
    { status: RequestStatus.Closed, colour: '#94a3b8' },
    { status: RequestStatus.Transferred, colour: '#8b5cf6' },
];

/**
 * Workflow Requests Dashboard: replaces the legacy ESS workflow requests dashboard. Built like the other dashboards
 * from the same shared pieces. Counts requests raised by the employees in scope, by status, type and department, and
 * shows which open ones are overdue and whom they are waiting with.
 *
 * A type filters every tile; a month, status, band or approver lists the requests; a department drills into its units.
 */
@Component({
    templateUrl: './workflow-dashboard.component.html',
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
export class WorkflowDashboardComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(WorkflowDashboardServiceProxy);
    private readonly _router = inject(Router);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly paginator = viewChild<Paginator>('paginator');
    readonly detailPanel = viewChild<ElementRef<HTMLElement>>('detailPanel');
    readonly filterPanel = viewChild<Popover>('filterPanel');

    readonly Part = WorkflowDashboardExportPart;
    readonly Kind = WorkflowRequestListKind;
    readonly Status = RequestStatus;
    readonly presets = PERIOD_PRESETS;
    readonly isArabic = abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    readonly legendBelow = LegendPosition.Below;

    // ------------------------------------------------------------ filters

    readonly preset = signal<PeriodPreset>('yearToDate');
    readonly fromDate = signal<Date>(periodOf('yearToDate').from);
    readonly toDate = signal<Date>(periodOf('yearToDate').to);
    readonly organizationUnitId = signal<number | undefined>(undefined);
    readonly requestType = signal<string | undefined>(undefined);

    readonly draftPreset = signal<PeriodPreset>('yearToDate');
    readonly draftFrom = signal<Date>(new Date());
    readonly draftTo = signal<Date>(new Date());
    readonly draftUnitId = signal<number | undefined>(undefined);

    // ------------------------------------------------------------ data

    readonly dashboard = signal<WorkflowDashboardDto | undefined>(undefined);
    readonly loading = signal(false);
    readonly exporting = signal(false);

    readonly detail = signal<RequestList | undefined>(undefined);
    readonly rows = signal<WorkflowRequestRowDto[]>([]);
    readonly rowsTotal = signal(0);
    readonly rowsLoading = signal(false);

    private _dashboardRequest: Subscription | undefined;

    // ------------------------------------------------------------ chart data

    readonly singleColour: Color = { name: 'wfSingle', selectable: true, group: ScaleType.Ordinal, domain: ['#3b82f6'] };
    readonly waitColour: Color = { name: 'wfWait', selectable: true, group: ScaleType.Ordinal, domain: ['#f59e0b'] };
    readonly bandColour: Color = {
        name: 'wfBand',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#10b981', '#84cc16', '#f59e0b', '#f97316', '#ef4444'],
    };

    readonly statusColours = computed(() => STATUSES.map((s) => ({ name: this.statusLabel(s.status), value: s.colour })));

    readonly trendResults = computed(() =>
        (this.dashboard()?.trend ?? []).map((p) => ({
            name: p.monthStart.toFormat('MMM yyyy'),
            series: STATUSES.map((s) => ({
                name: this.statusLabel(s.status),
                value: p.counts.find((c) => c.status === s.status)?.count ?? 0,
            })).filter((v) => v.value > 0),
        }))
    );

    readonly hasData = computed(() => (this.dashboard()?.kpis.raised ?? 0) > 0);

    readonly typeResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.byType ?? []).map((b) => ({ name: this.typeName(b.requestType), value: b.raised }))
    );

    readonly orgUnitResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.byOrgUnit ?? []).map((b) => ({ name: this.orgUnitLabel(b), value: b.raised }))
    );

    readonly waitingResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.waiting ?? []).map((w) => ({ name: this.approverLabel(w), value: w.waiting }))
    );

    readonly bandResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.waitingBands ?? []).map((b) => ({ name: this.bandLabel(b), value: b.requests }))
    );

    readonly decided = computed(() => {
        const k = this.dashboard()?.kpis;
        return k ? k.approved + k.rejected : 0;
    });

    readonly scopeLabel = computed(() => scopeLabel(this.dashboard()?.scope, (key) => this.l(key)));
    readonly scopeIsNobody = computed(() => this.dashboard()?.scope?.reason === DashboardScopeReason.Nobody);

    readonly selectedTypeName = computed(() => {
        const code = this.requestType();
        return code ? this.typeName(code) : '';
    });

    readonly detailTitle = computed(() => {
        const list = this.detail();
        switch (list?.kind) {
            case undefined:
                return '';
            case WorkflowRequestListKind.Status:
                return this.l('WorkflowDashboard_RequestsWithStatus', this.statusLabel(list.status ?? RequestStatus.Open));
            case WorkflowRequestListKind.Month:
                return this.l('WorkflowDashboard_RequestsInMonth', list.month?.toFormat('MMMM yyyy') ?? '');
            case WorkflowRequestListKind.Overdue:
                return this.l('WorkflowDashboard_OverdueList');
            case WorkflowRequestListKind.Approver:
                return this.l('WorkflowDashboard_WaitingWithList', list.approverName ?? '');
            case WorkflowRequestListKind.WaitingBand: {
                const band = (this.dashboard()?.waitingBands ?? []).find((b) => b.index === list.band);
                return this.l('WorkflowDashboard_WaitingBandList', band ? this.bandLabel(band) : '');
            }
            default:
                return this.l('WorkflowDashboard_AllRequests');
        }
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
            .getDashboard(this.buildInput(new WorkflowDashboardInput()))
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

        const input = this.buildInput(new GetWorkflowRequestsInput());
        input.kind = list.kind;
        input.status = list.status;
        input.month = list.month;
        input.approverId = list.approverId;
        input.band = list.band;
        input.skipCount = event?.first ?? 0;
        input.maxResultCount = event?.rows ?? this.paginator()?.rows ?? 10;

        this.rowsLoading.set(true);
        this._service
            .getRequests(input)
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

    private buildInput<T extends WorkflowDashboardInput>(input: T): T {
        input.fromDate = DateTime.fromJSDate(this.fromDate()).startOf('day');
        input.toDate = DateTime.fromJSDate(this.toDate()).startOf('day');
        input.organizationUnitId = this.organizationUnitId();
        input.requestType = this.requestType();
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
            this.showList({ kind: WorkflowRequestListKind.Month, month: point.monthStart });
        }
    }

    onTypeSelect(event: { name?: string }): void {
        const bucket = (this.dashboard()?.byType ?? []).find((b) => this.typeName(b.requestType) === event.name);
        if (bucket) {
            this.requestType.set(bucket.requestType);
            this.refresh();
        }
    }

    clearType(): void {
        this.requestType.set(undefined);
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

    onApproverSelect(event: { name?: string }): void {
        const approver = (this.dashboard()?.waiting ?? []).find((w) => this.approverLabel(w) === event.name);
        if (approver) {
            this.showList({
                kind: WorkflowRequestListKind.Approver,
                approverId: approver.approverId,
                approverName: this.pick(approver.approverNameEn, approver.approverNameAr),
            });
        }
    }

    onBandSelect(event: { name?: string }): void {
        const band = (this.dashboard()?.waitingBands ?? []).find((b) => this.bandLabel(b) === event.name);
        if (band) {
            this.showList({ kind: WorkflowRequestListKind.WaitingBand, band: band.index });
        }
    }

    clearAll(): void {
        this.organizationUnitId.set(undefined);
        this.requestType.set(undefined);
        this.refresh();
    }

    // ------------------------------------------------------------ lists and export

    showList(list: RequestList): void {
        const wasOpen = this.detail() !== undefined;
        this.detail.set(list);
        this.rows.set([]);

        if (wasOpen) {
            this.reloadRows();
        } else {
            this.getRows({ first: 0, rows: 10 });
        }

        setTimeout(() => this.detailPanel()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }

    showStatus(status: RequestStatus): void {
        this.showList({ kind: WorkflowRequestListKind.Status, status });
    }

    isListing(kind: WorkflowRequestListKind, status?: RequestStatus): boolean {
        const list = this.detail();
        return list?.kind === kind && (status === undefined || list.status === status);
    }

    closeList(): void {
        this.detail.set(undefined);
    }

    exportToExcel(part: WorkflowDashboardExportPart): void {
        const input = this.buildInput(new ExportWorkflowDashboardInput());
        input.part = part;
        const list = this.detail();
        if (part === WorkflowDashboardExportPart.Requests && list) {
            input.kind = list.kind;
            input.status = list.status;
            input.month = list.month;
            input.approverId = list.approverId;
            input.band = list.band;
        }

        this.exporting.set(true);
        this._service
            .exportToExcel(input)
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    openEmployee(employeeId: number | undefined): void {
        if (employeeId && this.permission.isGranted('Pages.PersonnelAdministration.Employees')) {
            this._router.navigate(['/app/admin/employees', employeeId]);
        }
    }

    // ------------------------------------------------------------ labels

    pick(en?: string, ar?: string): string {
        return (this.isArabic ? ar || en : en || ar) ?? '';
    }

    typeName(code: string | undefined): string {
        const type = (this.dashboard()?.types ?? []).find((t) => t.code === code);
        return type ? this.pick(type.nameEn, type.nameAr) : (code ?? '');
    }

    /** Closed is how a request the employee withdrew ends up. */
    statusLabel(status: RequestStatus): string {
        return status === RequestStatus.Closed ? this.l('WorkflowDashboard_Withdrawn') : this.l(RequestStatus[status]);
    }

    approverLabel(w: WorkflowApproverBucketDto): string {
        return `${this.pick(w.approverNameEn, w.approverNameAr)} (${w.approverCode})`;
    }

    bandLabel(band: WorkflowWaitingBandDto): string {
        return band.toDays != null
            ? this.l('WorkflowDashboard_BandRange', band.fromDays, band.toDays)
            : this.l('WorkflowDashboard_BandFrom', band.fromDays);
    }

    orgUnitLabel(bucket: WorkflowOrgUnitBucketDto): string {
        if (bucket.organizationUnitId == null) {
            return this.l('LeaveDashboard_Unassigned');
        }
        const name = bucket.name ?? '#' + bucket.organizationUnitId;
        return bucket.isDirectStaff ? this.l('LeaveDashboard_DirectStaff', name) : name;
    }

    indent(depth: number): string {
        return '   '.repeat(depth);
    }
}
