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
    ExportPayrollDashboardInput,
    GetPayrollCostCardInput,
    GetPayrollEmployeesInput,
    PaymentType,
    PayrollBasicBandDto,
    PayrollCostCardDto,
    PayrollCostComponent,
    PayrollDashboardDto,
    PayrollDashboardExportPart,
    PayrollDashboardInput,
    PayrollDashboardListKind,
    PayrollDashboardServiceProxy,
    PayrollEmployeeRowDto,
    PayrollOrgUnitBucketDto,
    PayrollRunType,
    PayrollWageTypeAmountDto,
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

/** What the payslip list is showing, and for which month, band, method or run type. */
interface PayslipList {
    kind: PayrollDashboardListKind;
    month?: DateTime;
    band?: number;
    paymentType?: PaymentType;
    runType?: PayrollRunType;
}

/**
 * Payroll Dashboard: replaces the legacy ESS Payroll and CTC dashboards. Built like the other dashboards from the same
 * shared pieces. Money is what payroll calculated, from each month's own result; cost to company adds the employer's
 * social security and the month's provisions to gross pay.
 *
 * A month, band, payment method or run type lists its payslips; a payslip opens that employee's cost card; a
 * department drills into its units.
 */
@Component({
    templateUrl: './payroll-dashboard.component.html',
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
export class PayrollDashboardComponent extends AppComponentBase implements OnInit {
    private readonly _service = inject(PayrollDashboardServiceProxy);
    private readonly _router = inject(Router);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly paginator = viewChild<Paginator>('paginator');
    readonly detailPanel = viewChild<ElementRef<HTMLElement>>('detailPanel');
    readonly cardPanel = viewChild<ElementRef<HTMLElement>>('cardPanel');
    readonly filterPanel = viewChild<Popover>('filterPanel');

    readonly Part = PayrollDashboardExportPart;
    readonly Kind = PayrollDashboardListKind;
    readonly presets = PERIOD_PRESETS;
    readonly isArabic = abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    readonly legendBelow = LegendPosition.Below;

    // ------------------------------------------------------------ filters

    readonly preset = signal<PeriodPreset>('yearToDate');
    readonly fromDate = signal<Date>(periodOf('yearToDate').from);
    readonly toDate = signal<Date>(periodOf('yearToDate').to);
    readonly organizationUnitId = signal<number | undefined>(undefined);
    readonly employeeId = signal<number | undefined>(undefined);

    readonly draftPreset = signal<PeriodPreset>('yearToDate');
    readonly draftFrom = signal<Date>(new Date());
    readonly draftTo = signal<Date>(new Date());
    readonly draftUnitId = signal<number | undefined>(undefined);

    // ------------------------------------------------------------ data

    readonly dashboard = signal<PayrollDashboardDto | undefined>(undefined);
    readonly loading = signal(false);
    readonly exporting = signal(false);

    readonly detail = signal<PayslipList | undefined>(undefined);
    readonly rows = signal<PayrollEmployeeRowDto[]>([]);
    readonly rowsTotal = signal(0);
    readonly rowsLoading = signal(false);

    readonly card = signal<PayrollCostCardDto | undefined>(undefined);
    readonly cardLoading = signal(false);

    private _dashboardRequest: Subscription | undefined;

    // ------------------------------------------------------------ chart data

    readonly singleColour: Color = { name: 'paySingle', selectable: true, group: ScaleType.Ordinal, domain: ['#3b82f6'] };
    readonly bandColour: Color = { name: 'payBand', selectable: true, group: ScaleType.Ordinal, domain: ['#8b5cf6'] };
    readonly earningColour: Color = { name: 'payEarn', selectable: true, group: ScaleType.Ordinal, domain: ['#10b981'] };
    readonly deductionColour: Color = { name: 'payDed', selectable: true, group: ScaleType.Ordinal, domain: ['#ef4444'] };
    readonly mixColour: Color = {
        name: 'payMix',
        selectable: true,
        group: ScaleType.Ordinal,
        domain: ['#3b82f6', '#06b6d4', '#f59e0b', '#8b5cf6', '#10b981', '#f97316'],
    };
    readonly pieColour: Color = { name: 'payPie', selectable: true, group: ScaleType.Ordinal, domain: ['#3b82f6', '#f59e0b', '#94a3b8'] };

    /** Net, deductions and employer cost stack up to cost to company. */
    readonly trendColours = computed(() => [
        { name: this.l('PayrollDashboard_Net'), value: '#3b82f6' },
        { name: this.l('PayrollDashboard_Deductions'), value: '#ef4444' },
        { name: this.l('PayrollDashboard_EmployerCost'), value: '#f59e0b' },
    ]);

    readonly trendResults = computed(() =>
        (this.dashboard()?.trend ?? []).map((p) => ({
            name: p.monthStart.toFormat('MMM yyyy'),
            series: [
                { name: this.l('PayrollDashboard_Net'), value: p.net },
                { name: this.l('PayrollDashboard_Deductions'), value: p.deductions },
                { name: this.l('PayrollDashboard_EmployerCost'), value: p.employerCost },
            ],
        }))
    );

    readonly hasData = computed(() => (this.dashboard()?.kpis.costToCompany ?? 0) !== 0);

    readonly mixResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.costMix ?? []).map((c) => ({ name: this.componentLabel(c.component), value: c.amount }))
    );

    readonly orgUnitResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.byOrgUnit ?? []).map((b) => ({ name: this.orgUnitLabel(b), value: b.costToCompany }))
    );

    readonly bandResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.basicBands ?? []).map((b) => ({ name: this.bandLabel(b), value: b.employees }))
    );

    readonly earningResults = computed<ChartItem[]>(() => (this.dashboard()?.earnings ?? []).map((w) => ({ name: this.wageLabel(w), value: w.amount })));

    readonly deductionResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.deductions ?? []).map((w) => ({ name: this.wageLabel(w), value: w.amount }))
    );

    readonly paymentResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.paymentMethods ?? []).map((b) => ({ name: this.paymentLabel(b.paymentType), value: b.employees }))
    );

    readonly runTypeResults = computed<ChartItem[]>(() =>
        (this.dashboard()?.runTypes ?? []).map((b) => ({ name: this.runTypeLabel(b.runType), value: b.net }))
    );

    readonly latestMonthLabel = computed(() => this.dashboard()?.latestMonth?.toFormat('MMMM yyyy') ?? '—');

    readonly deductionsPercent = computed(() => {
        const k = this.dashboard()?.kpis;
        return k && k.gross ? (k.deductions * 100) / k.gross : 0;
    });

    readonly scopeLabel = computed(() => scopeLabel(this.dashboard()?.scope, (key) => this.l(key)));
    readonly scopeIsNobody = computed(() => this.dashboard()?.scope?.reason === DashboardScopeReason.Nobody);

    readonly employeeName = computed(() => {
        const d = this.dashboard();
        return d ? this.pick(d.employeeNameEn, d.employeeNameAr) : '';
    });

    readonly detailTitle = computed(() => {
        const list = this.detail();
        const month = this.latestMonthLabel();
        switch (list?.kind) {
            case undefined:
                return '';
            case PayrollDashboardListKind.BasicBand: {
                const band = (this.dashboard()?.basicBands ?? []).find((b) => b.index === list.band);
                return this.l('PayrollDashboard_PayslipsInBand', band ? this.bandLabel(band) : '', month);
            }
            case PayrollDashboardListKind.PaymentMethod:
                return this.l('PayrollDashboard_PayslipsByMethod', this.paymentLabel(list.paymentType), month);
            case PayrollDashboardListKind.RunType:
                return this.l('PayrollDashboard_PayslipsByRunType', this.runTypeLabel(list.runType ?? PayrollRunType.Regular));
            default:
                return this.l('PayrollDashboard_PayslipsInMonth', list.month ? list.month.toFormat('MMMM yyyy') : month);
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
        this.card.set(undefined);

        const request = this._service
            .getDashboard(this.buildInput(new PayrollDashboardInput()))
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

        const input = this.buildInput(new GetPayrollEmployeesInput());
        input.kind = list.kind;
        input.month = list.month;
        input.band = list.band;
        input.paymentType = list.paymentType;
        input.runType = list.runType;
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

    private buildInput<T extends PayrollDashboardInput>(input: T): T {
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
            this.showList({ kind: PayrollDashboardListKind.Month, month: point.monthStart });
        }
    }

    onBandSelect(event: { name?: string }): void {
        const band = (this.dashboard()?.basicBands ?? []).find((b) => this.bandLabel(b) === event.name);
        if (band) {
            this.showList({ kind: PayrollDashboardListKind.BasicBand, band: band.index });
        }
    }

    onPaymentSelect(event: { name?: string }): void {
        const bucket = (this.dashboard()?.paymentMethods ?? []).find((b) => this.paymentLabel(b.paymentType) === event.name);
        if (bucket) {
            this.showList({ kind: PayrollDashboardListKind.PaymentMethod, paymentType: bucket.paymentType });
        }
    }

    onRunTypeSelect(event: { name?: string }): void {
        const bucket = (this.dashboard()?.runTypes ?? []).find((b) => this.runTypeLabel(b.runType) === event.name);
        if (bucket) {
            this.showList({ kind: PayrollDashboardListKind.RunType, runType: bucket.runType });
        }
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

    /** From the cost card: every tile for this one employee. */
    focusEmployee(employeeId: number): void {
        this.employeeId.set(employeeId);
        this.refresh();
    }

    clearEmployee(): void {
        this.employeeId.set(undefined);
        this.refresh();
    }

    clearAll(): void {
        this.organizationUnitId.set(undefined);
        this.employeeId.set(undefined);
        this.refresh();
    }

    // ------------------------------------------------------------ lists, cost card and export

    showList(list: PayslipList): void {
        const wasOpen = this.detail() !== undefined;
        this.detail.set(list);
        this.rows.set([]);
        this.card.set(undefined);

        if (wasOpen) {
            this.reloadRows();
        } else {
            this.getRows({ first: 0, rows: 10 });
        }

        setTimeout(() => this.detailPanel()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }

    isListing(kind: PayrollDashboardListKind): boolean {
        const list = this.detail();
        return list?.kind === kind && !list.month;
    }

    closeList(): void {
        this.detail.set(undefined);
        this.card.set(undefined);
    }

    /** The clicked payslip's month, line by line, for that employee. */
    openCard(row: PayrollEmployeeRowDto): void {
        const input = new GetPayrollCostCardInput();
        input.employeeId = row.employeeId;
        input.month = row.month;
        input.toDate = DateTime.fromJSDate(this.toDate()).startOf('day');

        this.cardLoading.set(true);
        this._service
            .getCostCard(input)
            .pipe(finalize(() => this.cardLoading.set(false)))
            .subscribe((card) => {
                this.card.set(card);
                setTimeout(() => this.cardPanel()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' }));
            });
    }

    closeCard(): void {
        this.card.set(undefined);
    }

    exportToExcel(part: PayrollDashboardExportPart): void {
        const input = this.buildInput(new ExportPayrollDashboardInput());
        input.part = part;
        const list = this.detail();
        if (part === PayrollDashboardExportPart.Employees && list) {
            input.kind = list.kind;
            input.month = list.month;
            input.band = list.band;
            input.paymentType = list.paymentType;
            input.runType = list.runType;
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

    componentLabel(component: PayrollCostComponent): string {
        return this.l('PayrollDashboard_Component_' + PayrollCostComponent[component]);
    }

    bandLabel(band: PayrollBasicBandDto): string {
        const f = (n: number) => n.toLocaleString('en-US');
        return band.to != null ? this.l('PayrollDashboard_BandRange', f(band.from), f(band.to)) : this.l('PayrollDashboard_BandFrom', f(band.from));
    }

    wageLabel(w: PayrollWageTypeAmountDto): string {
        return `${this.pick(w.nameEn, w.nameAr)} (${w.code})`;
    }

    paymentLabel(type: PaymentType | undefined): string {
        switch (type) {
            case PaymentType.Bank:
                return this.l('PayrollDashboard_PaymentBank');
            case PaymentType.Cash:
                return this.l('PayrollDashboard_PaymentCash');
            default:
                return this.l('PayrollDashboard_PaymentNotSet');
        }
    }

    runTypeLabel(type: PayrollRunType): string {
        return type === PayrollRunType.OffCycle ? this.l('PayrollDashboard_OffCycle') : this.l('PayrollDashboard_Regular');
    }

    orgUnitLabel(bucket: PayrollOrgUnitBucketDto): string {
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
