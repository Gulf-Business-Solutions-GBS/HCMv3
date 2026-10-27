import { Component, ElementRef, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
    BarChartModule,
    Color,
    LegendPosition,
    LineChartModule,
    PieChartModule,
    ScaleType,
} from '@swimlane/ngx-charts';
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
    ExportWorkforceDashboardInput,
    Gender,
    GetWorkforceDocumentsInput,
    GetWorkforceEmployeesInput,
    GetWorkforceMovementsInput,
    WorkforceAgeBand,
    WorkforceDashboardDto,
    WorkforceDashboardExportPart,
    WorkforceDashboardInput,
    WorkforceDashboardServiceProxy,
    WorkforceDocumentRowDto,
    WorkforceDocumentStatus,
    WorkforceEmployeeListKind,
    WorkforceEmployeeRowDto,
    WorkforceMovementKind,
    WorkforceMovementRowDto,
    WorkforceNamedCountDto,
    WorkforceOrgUnitBucketDto,
    WorkforceServiceBand,
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

/** A clicked-bar filter: what it sets on the input, and the chip text. */
interface AttributeFilter {
    gender?: Gender;
    nationalityId?: number;
    ageBand?: WorkforceAgeBand;
    serviceBand?: WorkforceServiceBand;
    qualificationId?: number;
    jobTitleId?: number;
    label: string;
}

/** What the movements-and-documents list is showing. */
type ActivityList =
    | { type: 'movement'; kind: WorkforceMovementKind }
    | { type: 'document'; documentTypeId?: number; status?: WorkforceDocumentStatus; typeName?: string };

/**
 * Workforce Dashboard: replaces the legacy ESS Workforce dashboard (age, department, gender, nationality, years of
 * service, qualification, job title) and adds joiners, leavers and turnover, which legacy never computed correctly.
 *
 * Built like the Leave Dashboard, from the same shared pieces (dashboard-shared.ts, dashboard.less,
 * CountUpDirective): filters in a window behind the Filter icon, Excel per tile or whole, tiles updating in place.
 * Clicking a bar narrows every tile to those employees and shows as a removable chip; several can be combined.
 * Whose employees are shown is decided by the server (IDashboardScopeService) and stated in the toolbar.
 */
@Component({
    templateUrl: './workforce-dashboard.component.html',
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
        LineChartModule,
        PieChartModule,
        Popover,
        CountUpDirective,
        DashboardExportMenuComponent,
        HelpPageDirective,
    ],
})
export class WorkforceDashboardComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(WorkforceDashboardServiceProxy);
    private readonly _router = inject(Router);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly paginator = viewChild<Paginator>('paginator');
    readonly detailPanel = viewChild<ElementRef<HTMLElement>>('detailPanel');
    readonly activityPaginator = viewChild<Paginator>('activityPaginator');
    readonly activityPanel = viewChild<ElementRef<HTMLElement>>('activityPanel');
    readonly filterPanel = viewChild<Popover>('filterPanel');

    readonly Part = WorkforceDashboardExportPart;
    readonly Kind = WorkforceEmployeeListKind;
    readonly DocumentStatus = WorkforceDocumentStatus;
    readonly presets = PERIOD_PRESETS;
    readonly isArabic = abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    readonly legendBelow = LegendPosition.Below;

    // ------------------------------------------------------------ filters

    readonly preset = signal<PeriodPreset>('yearToDate');
    readonly fromDate = signal<Date>(periodOf('yearToDate').from);
    readonly toDate = signal<Date>(periodOf('yearToDate').to);
    readonly organizationUnitId = signal<number | undefined>(undefined);
    readonly filters = signal<AttributeFilter[]>([]);

    readonly draftPreset = signal<PeriodPreset>('yearToDate');
    readonly draftFrom = signal<Date>(new Date());
    readonly draftTo = signal<Date>(new Date());
    readonly draftUnitId = signal<number | undefined>(undefined);

    // ------------------------------------------------------------ data

    readonly dashboard = signal<WorkforceDashboardDto | undefined>(undefined);
    readonly loading = signal(false);
    readonly exporting = signal(false);

    readonly detailKind = signal<WorkforceEmployeeListKind | undefined>(undefined);
    readonly rows = signal<WorkforceEmployeeRowDto[]>([]);
    readonly rowsTotal = signal(0);
    readonly rowsLoading = signal(false);

    readonly activity = signal<ActivityList | undefined>(undefined);
    readonly movementRows = signal<WorkforceMovementRowDto[]>([]);
    readonly documentRows = signal<WorkforceDocumentRowDto[]>([]);
    readonly activityTotal = signal(0);
    readonly activityLoading = signal(false);

    private _dashboardRequest: Subscription | undefined;

    // ------------------------------------------------------------ chart data

    readonly singleColour: Color = {
        name: 'wfSingle',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#3b82f6'],
    };
    readonly ageColour: Color = { name: 'wfAge', selectable: true, group: ScaleType.Ordinal, domain: ['#8b5cf6'] };
    readonly serviceColour: Color = {
        name: 'wfService',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#10b981'],
    };
    readonly joinLeaveColour: Color = {
        name: 'wfJoinLeave',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#10b981', '#ef4444'],
    };
    readonly turnoverColour: Color = {
        name: 'wfTurnover',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#f59e0b'],
    };
    readonly genderColours = computed(() => [
        { name: this.l('Male'), value: '#3b82f6' },
        { name: this.l('Female'), value: '#ec4899' },
        { name: this.l('WorkforceDashboard_GenderNotSpecified'), value: '#94a3b8' },
    ]);

    readonly joinLeaveResults = computed(() =>
        (this.dashboard()?.trend ?? []).map((p) => ({
            name: p.monthStart.toFormat('MMM yyyy'),
            series: [
                { name: this.l('WorkforceDashboard_Joiners'), value: p.joiners },
                { name: this.l('WorkforceDashboard_Leavers'), value: p.leavers },
            ],
        }))
    );

    readonly turnoverResults = computed(() => [
        {
            name: this.l('WorkforceDashboard_Turnover'),
            series: (this.dashboard()?.trend ?? []).map((p) => ({
                name: p.monthStart.toFormat('MMM yyyy'),
                value: p.turnoverPercent ?? 0,
            })),
        },
    ]);

    readonly movementColour: Color = {
        name: 'wfMovement',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#f59e0b', '#10b981', '#ef4444', '#3b82f6', '#8b5cf6'],
    };

    readonly documentColours = computed(() => [
        { name: this.l('WorkforceDashboard_Document_Expired'), value: '#ef4444' },
        { name: this.l('WorkforceDashboard_Document_Expiring'), value: '#f59e0b' },
    ]);

    readonly movementResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.movements ?? []).map((m) => ({ name: this.movementLabel(m.kind), value: m.count }))
    );

    readonly hasMovements = computed(() => (this.dashboard()?.movements ?? []).some((m) => m.count > 0));

    readonly documentResults = computed(() =>
        (this.dashboard()?.documents ?? []).map((d) => ({
            name: this.pick(d.nameEn, d.nameAr),
            series: [
                { name: this.l('WorkforceDashboard_Document_Expired'), value: d.expired },
                { name: this.l('WorkforceDashboard_Document_Expiring'), value: d.expiring },
            ],
        }))
    );

    readonly activityTitle = computed(() => {
        const list = this.activity();
        if (!list) {
            return '';
        }
        if (list.type === 'movement') {
            return this.movementLabel(list.kind);
        }
        if (list.typeName && list.status !== undefined) {
            return this.l('WorkforceDashboard_DocumentsOfType', list.typeName, this.documentStatusLabel(list.status));
        }
        return this.l('WorkforceDashboard_DocumentsList');
    });

    readonly hasMovement = computed(() => (this.dashboard()?.trend ?? []).some((p) => p.joiners > 0 || p.leavers > 0));

    readonly ageResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.ageBands ?? []).map((b) => ({ name: this.ageLabel(b.band), value: b.count }))
    );

    readonly serviceResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.serviceBands ?? []).map((b) => ({ name: this.serviceLabel(b.band), value: b.count }))
    );

    readonly genderResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.genders ?? []).map((g) => ({ name: this.genderLabel(g.gender), value: g.count }))
    );

    readonly nationalityResults = computed<ChartItem[]>(() => this.named(this.dashboard()?.nationalities));
    readonly qualificationResults = computed<ChartItem[]>(() => this.named(this.dashboard()?.qualifications));
    readonly jobTitleResults = computed<ChartItem[]>(() => this.named(this.dashboard()?.jobTitles));

    readonly orgUnitResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.byOrgUnit ?? []).map((b) => ({ name: this.orgUnitLabel(b), value: b.headcount }))
    );

    readonly femalePercent = computed(() => {
        const k = this.dashboard()?.kpis;
        return k && k.headcount ? Math.round((k.female / k.headcount) * 100) : 0;
    });

    readonly malePercent = computed(() => {
        const k = this.dashboard()?.kpis;
        return k && k.headcount ? Math.round((k.male / k.headcount) * 100) : 0;
    });

    readonly scopeLabel = computed(() => scopeLabel(this.dashboard()?.scope, (key) => this.l(key)));
    readonly scopeIsNobody = computed(() => this.dashboard()?.scope?.reason === DashboardScopeReason.Nobody);

    readonly detailTitle = computed(() => {
        switch (this.detailKind()) {
            case WorkforceEmployeeListKind.Joiners:
                return this.l('WorkforceDashboard_Joiners');
            case WorkforceEmployeeListKind.Leavers:
                return this.l('WorkforceDashboard_Leavers');
            default:
                return this.l('WorkforceDashboard_Headcount');
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
            .getDashboard(this.buildInput(new WorkforceDashboardInput()))
            .pipe(
                finalize(() => {
                    if (this._dashboardRequest === request) {
                        this.loading.set(false);
                    }
                })
            )
            .subscribe((dto) => {
                this.dashboard.set(dto);
                if (this.detailKind() !== undefined) {
                    this.reloadRows();
                }
                if (this.activity()) {
                    this.activityPaginator()?.changePage(0);
                    this.getActivityRows({ first: 0, rows: this.activityPaginator()?.rows ?? 10 });
                }
            });
        this._dashboardRequest = request;
    }

    getRows(event?: LazyLoadEvent): void {
        const kind = this.detailKind();
        if (kind === undefined) {
            return;
        }

        const input = this.buildInput(new GetWorkforceEmployeesInput());
        input.kind = kind;
        input.skipCount = event?.first ?? 0;
        input.maxResultCount = event?.rows ?? this.paginator()?.rows ?? 10;

        this.rowsLoading.set(true);
        this._service
            .getEmployees(input)
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

    /** Copies the applied filters onto any of the three input types. */
    private buildInput<T extends WorkforceDashboardInput>(input: T): T {
        input.fromDate = DateTime.fromJSDate(this.fromDate()).startOf('day');
        input.toDate = DateTime.fromJSDate(this.toDate()).startOf('day');
        input.organizationUnitId = this.organizationUnitId();
        for (const f of this.filters()) {
            input.gender = f.gender ?? input.gender;
            input.nationalityId = f.nationalityId ?? input.nationalityId;
            input.ageBand = f.ageBand ?? input.ageBand;
            input.serviceBand = f.serviceBand ?? input.serviceBand;
            input.qualificationId = f.qualificationId ?? input.qualificationId;
            input.jobTitleId = f.jobTitleId ?? input.jobTitleId;
        }
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

    onAgeSelect(event: { name?: string }): void {
        const band = (this.dashboard()?.ageBands ?? []).find((b) => this.ageLabel(b.band) === event.name);
        if (band) {
            this.addFilter({ ageBand: band.band, label: this.ageLabel(band.band) });
        }
    }

    onServiceSelect(event: { name?: string }): void {
        const band = (this.dashboard()?.serviceBands ?? []).find((b) => this.serviceLabel(b.band) === event.name);
        if (band) {
            this.addFilter({ serviceBand: band.band, label: this.serviceLabel(band.band) });
        }
    }

    onGenderSelect(event: { name?: string }): void {
        const g = (this.dashboard()?.genders ?? []).find((x) => this.genderLabel(x.gender) === event.name);
        if (g) {
            this.addFilter({ gender: g.gender, label: this.genderLabel(g.gender) });
        }
    }

    onNamedSelect(kind: 'nationality' | 'qualification' | 'jobTitle', event: { name?: string }): void {
        const source =
            kind === 'nationality'
                ? this.dashboard()?.nationalities
                : kind === 'qualification'
                  ? this.dashboard()?.qualifications
                  : this.dashboard()?.jobTitles;
        const item = (source ?? []).find((x) => this.namedLabel(x) === event.name);
        // "Others" is many groups rolled together, so there is nothing single to filter by.
        if (!item || item.isOthers) {
            return;
        }

        const label = this.namedLabel(item);
        if (kind === 'nationality') {
            this.addFilter({ nationalityId: item.id, label });
        } else if (kind === 'qualification') {
            this.addFilter({ qualificationId: item.id, label });
        } else {
            this.addFilter({ jobTitleId: item.id, label });
        }
    }

    /** Adds a clicked-bar filter, replacing any earlier one on the same attribute. */
    private addFilter(filter: AttributeFilter): void {
        const key = (f: AttributeFilter) => Object.keys(f).find((k) => k !== 'label');
        this.filters.set([...this.filters().filter((f) => key(f) !== key(filter)), filter]);
        this.refresh();
    }

    removeFilter(filter: AttributeFilter): void {
        this.filters.set(this.filters().filter((f) => f !== filter));
        this.refresh();
    }

    clearAll(): void {
        this.filters.set([]);
        this.organizationUnitId.set(undefined);
        this.refresh();
    }

    // ------------------------------------------------------------ lists and export

    showList(kind: WorkforceEmployeeListKind): void {
        const wasOpen = this.detailKind() !== undefined;
        this.detailKind.set(kind);

        // Switching lists keeps the paginator mounted, so it will not fire again by itself.
        if (wasOpen) {
            this.reloadRows();
        } else {
            this.getRows({ first: 0, rows: 10 });
        }

        setTimeout(() => this.detailPanel()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }

    closeList(): void {
        this.detailKind.set(undefined);
    }

    onMovementSelect(event: { name?: string }): void {
        const count = (this.dashboard()?.movements ?? []).find((m) => this.movementLabel(m.kind) === event.name);
        if (count) {
            this.showActivity({ type: 'movement', kind: count.kind });
        }
    }

    /** A bar segment is a type and a status; a legend click is a status across every type. */
    onDocumentSelect(event: { name?: string; series?: string }): void {
        const status = [WorkforceDocumentStatus.Expired, WorkforceDocumentStatus.Expiring].find(
            (s) => this.documentStatusLabel(s) === event.name
        );
        const type = (this.dashboard()?.documents ?? []).find((d) => this.pick(d.nameEn, d.nameAr) === event.series);
        this.showActivity({
            type: 'document',
            documentTypeId: type?.documentTypeId,
            typeName: type ? this.pick(type.nameEn, type.nameAr) : undefined,
            status,
        });
    }

    showActivity(list: ActivityList): void {
        const wasOpen = this.activity() !== undefined;
        this.activity.set(list);
        this.movementRows.set([]);
        this.documentRows.set([]);

        if (wasOpen) {
            this.activityPaginator()?.changePage(0);
        }
        this.getActivityRows({ first: 0, rows: this.activityPaginator()?.rows ?? 10 });

        setTimeout(() => this.activityPanel()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }

    getActivityRows(event?: LazyLoadEvent): void {
        const list = this.activity();
        if (!list) {
            return;
        }

        const skip = event?.first ?? 0;
        const take = event?.rows ?? this.activityPaginator()?.rows ?? 10;
        this.activityLoading.set(true);

        if (list.type === 'movement') {
            const input = this.buildInput(new GetWorkforceMovementsInput());
            input.kind = list.kind;
            input.skipCount = skip;
            input.maxResultCount = take;
            this._service
                .getMovements(input)
                .pipe(finalize(() => this.activityLoading.set(false)))
                .subscribe((result) => {
                    this.activityTotal.set(result.totalCount);
                    this.movementRows.set(result.items);
                });
        } else {
            const input = this.buildInput(new GetWorkforceDocumentsInput());
            input.documentTypeId = list.documentTypeId;
            input.status = list.status;
            input.skipCount = skip;
            input.maxResultCount = take;
            this._service
                .getDocuments(input)
                .pipe(finalize(() => this.activityLoading.set(false)))
                .subscribe((result) => {
                    this.activityTotal.set(result.totalCount);
                    this.documentRows.set(result.items);
                });
        }
    }

    closeActivity(): void {
        this.activity.set(undefined);
    }

    exportActivity(): void {
        this.exportToExcel(
            this.activity()?.type === 'document'
                ? WorkforceDashboardExportPart.Documents
                : WorkforceDashboardExportPart.Movements
        );
    }

    exportToExcel(part: WorkforceDashboardExportPart): void {
        const input = this.buildInput(new ExportWorkforceDashboardInput());
        input.part = part;

        this.exporting.set(true);
        this._service
            .exportToExcel(input)
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    exportList(): void {
        switch (this.detailKind()) {
            case WorkforceEmployeeListKind.Joiners:
                return this.exportToExcel(WorkforceDashboardExportPart.Joiners);
            case WorkforceEmployeeListKind.Leavers:
                return this.exportToExcel(WorkforceDashboardExportPart.Leavers);
            default:
                return this.exportToExcel(WorkforceDashboardExportPart.Headcount);
        }
    }

    openEmployee(row: { employeeId: number }): void {
        if (this.permission.isGranted('Pages.PersonnelAdministration.Employees')) {
            this._router.navigate(['/app/admin/employees', row.employeeId]);
        }
    }

    // ------------------------------------------------------------ labels

    pick(en?: string, ar?: string): string {
        return (this.isArabic ? ar || en : en || ar) ?? '';
    }

    movementLabel(kind: WorkforceMovementKind): string {
        return this.l('WorkforceDashboard_Movement_' + WorkforceMovementKind[kind]);
    }

    documentStatusLabel(status: WorkforceDocumentStatus): string {
        return this.l('WorkforceDashboard_Document_' + WorkforceDocumentStatus[status]);
    }

    ageLabel(band: WorkforceAgeBand): string {
        return this.l('WorkforceAgeBand_' + WorkforceAgeBand[band]);
    }

    serviceLabel(band: WorkforceServiceBand): string {
        return this.l('WorkforceServiceBand_' + WorkforceServiceBand[band]);
    }

    genderLabel(gender: Gender): string {
        switch (gender) {
            case Gender.Male:
                return this.l('Male');
            case Gender.Female:
                return this.l('Female');
            default:
                return this.l('WorkforceDashboard_GenderNotSpecified');
        }
    }

    namedLabel(item: WorkforceNamedCountDto): string {
        if (item.isOthers) {
            return this.l('Others');
        }
        return item.id === 0 ? this.l('WorkforceDashboard_NotRecorded') : this.pick(item.nameEn, item.nameAr);
    }

    orgUnitLabel(bucket: WorkforceOrgUnitBucketDto): string {
        if (bucket.organizationUnitId == null) {
            return this.l('LeaveDashboard_Unassigned');
        }
        const name = bucket.name ?? '#' + bucket.organizationUnitId;
        return bucket.isDirectStaff ? this.l('LeaveDashboard_DirectStaff', name) : name;
    }

    indent(depth: number): string {
        return '   '.repeat(depth);
    }

    private named(items: WorkforceNamedCountDto[] | undefined): ChartItem[] {
        return (items ?? []).map((x) => ({ name: this.namedLabel(x), value: x.count }));
    }
}
