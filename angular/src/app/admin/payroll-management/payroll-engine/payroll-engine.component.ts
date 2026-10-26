import { Component, OnDestroy, OnInit, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    EngineType,
    ExecutionLogDto,
    ExecutionLogModule,
    ExecutionLogStatus,
    ExecutionLogsServiceProxy,
    GetAllExecutionLogsInput,
    GetAllPayrollAreasInput,
    GetPayrollRunCandidatesInput,
    GetPayrollRunEmployeesToExcelInput,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    PayrollEngineServiceProxy,
    PayrollRetroReviewServiceProxy,
    PayrollRunCandidateFilter,
    PayrollRunContextDto,
    PayrollRunOffCycleReasonDto,
    PayrollRunPeriodStatus,
    PayrollRunSetupServiceProxy,
    PayrollRunType,
    RunPayrollEngineInput,
    TenantUiSettingsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { EngineRunProgressComponent } from '@app/shared/common/engine-run-progress/engine-run-progress.component';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { PayrollRunEmployeePickerModalComponent } from './payroll-run-employee-picker-modal.component';
import { PayrollRetroReviewModalComponent } from './payroll-retro-review-modal.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Port of the legacy Payroll Engine run screen
 * (HCM10.Web/UserControls/PayrollManagement/uscPayrollEngine.ascx + .ascx.cs; uscPayrollEngine2.* is a
 * rename-only clone of the same screen and is intentionally not ported separately).
 *
 * Legacy shelled out to HCM10.PayrollEngine/Program.Main on a ThreadPool work item and polled a DevExpress
 * callback panel for progress. Here the run is enqueued as an ABP background job by
 * IPayrollEngineAppService.RunPayrollEngine, and progress is read from the ExecutionLog rows the engine
 * publishes (ExecutionLogModule.PayrollEngine).
 *
 * The screen guides the operator as legacy did (IPayrollRunSetupAppService): choosing a payroll area fills the
 * period from its control record, shows the payroll status, picks Regular (locked, green) or OffCycle (closed,
 * yellow) and the pay-in date the engine accepts; the employee picker lists who is eligible, Processed or Pending.
 *
 * Deliberately follows TimeEngineComponent one-for-one so both engine screens behave identically.
 */
@Component({
    templateUrl: './payroll-engine.component.html',
    animations: [appModuleAnimation()],
    imports: [
        HelpPageDirective,
        DateInputDirective,
        SubHeaderComponent,
        EngineRunProgressComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
        PayrollRunEmployeePickerModalComponent,
        PayrollRetroReviewModalComponent,
    ],
})
export class PayrollEngineComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _payrollEngineService = inject(PayrollEngineServiceProxy);
    private _executionLogsService = inject(ExecutionLogsServiceProxy);
    private _payrollAreasService = inject(PayrollAreasServiceProxy);
    private _payrollRunSetupService = inject(PayrollRunSetupServiceProxy);
    private _payrollRetroReviewService = inject(PayrollRetroReviewServiceProxy);
    private _tenantUiSettingsService = inject(TenantUiSettingsServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** The shared live-progress panel; told to start polling as soon as a run is queued. */
    readonly engineProgress = viewChild<EngineRunProgressComponent>('engineProgress');
    readonly employeePicker = viewChild<PayrollRunEmployeePickerModalComponent>('employeePicker');
    readonly retroReviewModal = viewChild<PayrollRetroReviewModalComponent>('retroReviewModal');

    readonly EngineType = EngineType;
    readonly PayrollRunType = PayrollRunType;
    readonly PayrollRunPeriodStatus = PayrollRunPeriodStatus;

    // Legacy args[0..6] -- see uscPayrollEngine.ascx.cs ExecuteLongOperation().
    readonly payrollRunType = signal<PayrollRunType>(PayrollRunType.Regular);
    readonly employeeCodes = signal<string>('');
    readonly payInDate = signal<string>('');
    readonly payrollAreaName = signal<string>('');
    readonly offCyclePaymentDate = signal<string>('');
    readonly offCycleReason = signal<string>('');

    readonly payrollAreas = signal<PayrollAreaDto[]>([]);
    readonly running = signal<boolean>(false);
    readonly latestLog = signal<ExecutionLogDto | undefined>(undefined);

    /** Legacy SetCurrentDates: what the chosen payroll area's control record says. */
    readonly context = signal<PayrollRunContextDto | undefined>(undefined);
    readonly contextLoading = signal<boolean>(false);

    /**
     * Legacy BindOffReasonType (the PayrollReasonType table). Each item's VALUE is the English name, which is what
     * the engine matches by value (PayrollOffCycleReasons).
     */
    readonly offCycleReasons = signal<PayrollRunOffCycleReasonDto[]>([]);

    // Legacy chkSimulation / rdCurrent / rdOtherPeriod / txtMonth / txtyear. Offered only when the tenant's payroll
    // setting AllowSimulationRun is on.
    readonly allowSimulationRun = signal<boolean>(false);
    readonly isSimulation = signal<boolean>(false);
    readonly simulationOtherPeriod = signal<boolean>(false);
    readonly simulationMonth = signal<number | undefined>(undefined);
    readonly simulationYear = signal<number | undefined>(undefined);
    readonly simulationStart = signal<DateTime | undefined>(undefined);
    readonly simulationEnd = signal<DateTime | undefined>(undefined);

    /** Legacy UpdateShowRetroButtonText: "Show Retro (n)". */
    readonly retroCount = signal<number>(0);

    readonly exporting = signal<boolean>(false);

    private _pollHandle: any;

    ngOnInit(): void {
        // The PrimeNG table's (onLazyLoad) fires the first getExecutionLogs() -- do not call it here.
        this._payrollAreasService
            .getAll(
                new GetAllPayrollAreasInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.payrollAreas.set(result.items));

        this._payrollRunSetupService.getOffCycleReasons().subscribe((result) => this.offCycleReasons.set(result || []));

        this._tenantUiSettingsService
            .getTenantUiSettings()
            .subscribe((settings) => this.allowSimulationRun.set(!!settings?.allowSimulationRun));
    }

    ngOnDestroy(): void {
        this.stopPolling();
    }

    isOffCycle(): boolean {
        return +this.payrollRunType() === PayrollRunType.OffCycle;
    }

    // --------------------------------------------------------------------------------------------------------------
    // Payroll area -> context (legacy ASPxCallbackPanelPeriod "PayrollArea" + SetCurrentDates)
    // --------------------------------------------------------------------------------------------------------------

    onPayrollAreaChange(value: string): void {
        this.payrollAreaName.set(value || '');

        // Legacy: choosing an area unticks Simulation and goes back to the current period.
        this.isSimulation.set(false);
        this.resetSimulationPeriod();

        this.employeeCodes.set('');
        this.context.set(undefined);
        this.retroCount.set(0);

        if (!value) {
            this.payInDate.set('');
            return;
        }

        this.contextLoading.set(true);
        this._payrollRunSetupService
            .getPayrollRunContext(value)
            .pipe(finalize(() => this.contextLoading.set(false)))
            .subscribe((context) => {
                // The user may have picked another area while this one loaded.
                if (this.payrollAreaName() !== value) {
                    return;
                }

                this.context.set(context);

                if (context.suggestedRunType !== undefined && context.suggestedRunType !== null) {
                    this.payrollRunType.set(context.suggestedRunType);
                }

                this.applyRunTypeDefaults();
            });

        this.refreshRetroCount();
    }

    onPayrollRunTypeChange(value: any): void {
        this.payrollRunType.set(+value as PayrollRunType);
        // Legacy SetReasonType: the employee list depends on the run type, so a previous pick no longer applies.
        this.employeeCodes.set('');
        this.applyRunTypeDefaults();
    }

    onOffCycleReasonChange(value: string): void {
        this.offCycleReason.set(value || '');
        this.employeeCodes.set('');
    }

    /** Legacy SetReasonType + the dates SetCurrentDates fills for the chosen run type. */
    private applyRunTypeDefaults(): void {
        const context = this.context();

        if (this.isOffCycle()) {
            // Legacy selects the first reason when the run turns off-cycle.
            if (!this.offCycleReason() && this.offCycleReasons().length) {
                this.offCycleReason.set(this.offCycleReasons()[0].value);
            }

            if (context?.hasControlRecord && context.showOffCyclePaymentDate && !this.offCyclePaymentDate()) {
                this.offCyclePaymentDate.set(this.toDateInput(context.defaultOffCyclePaymentDate));
            }
        } else {
            this.offCycleReason.set('');
            this.offCyclePaymentDate.set('');
        }

        this.applyPayInDate();
    }

    /**
     * The pay-in date the engine accepts: a regular run must be on the control record's period end; an off-cycle is
     * booked into the following period's month end (PayrollControlRecordGuard). A simulation on another period uses
     * that period's end.
     */
    private applyPayInDate(): void {
        const context = this.context();

        if (this.isSimulation() && this.simulationOtherPeriod()) {
            this.payInDate.set(this.toDateInput(this.simulationEnd()));
            return;
        }

        if (!context?.hasControlRecord) {
            this.payInDate.set('');
            return;
        }

        this.payInDate.set(this.toDateInput(this.isOffCycle() ? context.offCyclePayInDate : context.regularPayInDate));
    }

    /** Filled from the control record; typed by hand only when the area has none (the engine will refuse anyway). */
    payInDateReadOnly(): boolean {
        return !!this.context()?.hasControlRecord;
    }

    showOffCyclePaymentDate(): boolean {
        // Legacy hides it when time-engine autorun is on: the engine ignores it then.
        const context = this.context();
        return this.isOffCycle() && (!context || context.showOffCyclePaymentDate);
    }

    /** The engine accepts an off-cycle payment date only inside the period after the control record. */
    offCyclePaymentDateMin(): string | null {
        return this.toDateInput(this.context()?.offCyclePaymentDateFrom) || null;
    }

    offCyclePaymentDateMax(): string | null {
        return this.toDateInput(this.context()?.offCyclePaymentDateTo) || null;
    }

    runTypeColourClass(): string {
        if (!this.context()?.hasControlRecord) {
            return '';
        }

        // Legacy SetPayrollRunTypeColor: Regular light green, OffCycle light yellow.
        return this.isOffCycle() ? 'bg-light-warning' : 'bg-light-success';
    }

    statusBadgeClass(): string {
        switch (this.context()?.periodStatus) {
            case PayrollRunPeriodStatus.Regular:
                return 'badge badge-light-success fs-7';
            case PayrollRunPeriodStatus.OffCycle:
                return 'badge badge-light-warning fs-7';
            case PayrollRunPeriodStatus.DataCorrection:
                return 'badge badge-light-info fs-7';
            default:
                return 'badge badge-light-danger fs-7';
        }
    }

    statusText(): string {
        const context = this.context();
        if (!context) {
            return '';
        }

        return context.hasControlRecord ? context.periodStatusText : context.message;
    }

    // --------------------------------------------------------------------------------------------------------------
    // Simulation (legacy chkSimulation / rdCurrent / rdOtherPeriod)
    // --------------------------------------------------------------------------------------------------------------

    onSimulationChange(checked: boolean): void {
        this.isSimulation.set(!!checked);
        this.resetSimulationPeriod();
        this.applyPayInDate();
    }

    onSimulationPeriodChange(other: boolean): void {
        this.simulationOtherPeriod.set(other);

        if (other) {
            const context = this.context();
            this.simulationMonth.set(context?.payrollMonth ?? undefined);
            this.simulationYear.set(context?.payrollYear ?? undefined);
            this.loadSimulationDates();
        } else {
            this.simulationStart.set(undefined);
            this.simulationEnd.set(undefined);
            this.applyPayInDate();
        }
    }

    onSimulationMonthChange(value: any): void {
        this.simulationMonth.set(value ? +value : undefined);
        this.loadSimulationDates();
    }

    onSimulationYearChange(value: any): void {
        this.simulationYear.set(value ? +value : undefined);
        this.loadSimulationDates();
    }

    /** Legacy SetDates: the other month's start and end from the payroll area's payroll year. */
    private loadSimulationDates(): void {
        const month = this.simulationMonth();
        const year = this.simulationYear();

        this.simulationStart.set(undefined);
        this.simulationEnd.set(undefined);

        if (!this.payrollAreaName() || !month || !year || month < 1 || month > 12 || year < 1900) {
            this.applyPayInDate();
            return;
        }

        this._payrollRunSetupService
            .getPayrollRunPeriodDates(this.payrollAreaName(), month, year)
            .subscribe((dates) => {
                if (this.simulationMonth() !== month || this.simulationYear() !== year) {
                    return;
                }

                this.simulationStart.set(dates.calendarMonthStart);
                this.simulationEnd.set(dates.calendarMonthEnd);
                this.applyPayInDate();
            });
    }

    private resetSimulationPeriod(): void {
        this.simulationOtherPeriod.set(false);
        this.simulationMonth.set(undefined);
        this.simulationYear.set(undefined);
        this.simulationStart.set(undefined);
        this.simulationEnd.set(undefined);
    }

    private simulatingOtherPeriod(): boolean {
        return this.isSimulation() && this.simulationOtherPeriod();
    }

    // --------------------------------------------------------------------------------------------------------------
    // Employee picker (legacy employee popup)
    // --------------------------------------------------------------------------------------------------------------

    canPickEmployees(): boolean {
        return !!this.payrollAreaName() && (!this.isOffCycle() || !!this.offCycleReason());
    }

    openEmployeePicker(): void {
        if (!this.canPickEmployees()) {
            return;
        }

        this.employeePicker().show({
            payrollAreaName: this.payrollAreaName(),
            payrollRunType: +this.payrollRunType() as PayrollRunType,
            offCycleReason: this.isOffCycle() ? this.offCycleReason() : undefined,
            selectedCodes: this.employeeCodes(),
            periodStatusText: this.statusText(),
            simulationMonth: this.simulatingOtherPeriod() ? this.simulationMonth() : undefined,
            simulationYear: this.simulatingOtherPeriod() ? this.simulationYear() : undefined,
        });
    }

    onEmployeesPicked(codes: string): void {
        this.employeeCodes.set(codes || '');
    }

    // --------------------------------------------------------------------------------------------------------------
    // Retro (dialog owned by PayrollRetroReviewModalComponent)
    // --------------------------------------------------------------------------------------------------------------

    refreshRetroCount(): void {
        const area = this.payrollAreaName();
        if (!area) {
            this.retroCount.set(0);
            return;
        }

        this._payrollRetroReviewService.getRetroCount(area).subscribe((count) => {
            if (this.payrollAreaName() === area) {
                this.retroCount.set(count || 0);
            }
        });
    }

    showRetro(): void {
        if (this.payrollAreaName()) {
            this.retroReviewModal().show(this.payrollAreaName());
        }
    }

    // --------------------------------------------------------------------------------------------------------------
    // Export (legacy btnExportPayrollLog_Click)
    // --------------------------------------------------------------------------------------------------------------

    exportRunEmployeesToExcel(): void {
        const progress = this.engineProgress();

        this.exporting.set(true);
        this._payrollRunSetupService
            .getPayrollRunEmployeesToExcel(
                new GetPayrollRunEmployeesToExcelInput({
                    // Same run and same filters as the per-employee grid on screen.
                    runId: progress?.status()?.runId ?? undefined,
                    filter: progress?.filterText() ? progress.filterText() : undefined,
                    onlyFailed: progress?.onlyFailed(),
                })
            )
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    // --------------------------------------------------------------------------------------------------------------
    // Run
    // --------------------------------------------------------------------------------------------------------------

    runPayrollEngine(): void {
        // Same order the app service validates in (PayrollEngineAppService.RunPayrollEngine), so the client
        // surfaces the same message the server would have.
        if (!this.payInDate()) {
            this.message.warn(this.l('PayrollEnginePayInDateRequired'));
            return;
        }

        if (!this.payrollAreaName()) {
            this.message.warn(this.l('PayrollEnginePayrollAreaRequired'));
            return;
        }

        if (this.isOffCycle() && !this.offCycleReason()) {
            this.message.warn(this.l('PayrollEngineOffCycleReasonRequired'));
            return;
        }

        // Engine-level guard (legacy Program.cs lines 285-291/314-320): off-cycle runs always target a
        // specific employee set.
        if (this.isOffCycle() && !this.employeeCodes()) {
            this.message.warn(this.l('PayrollEngineEmployeeCodesRequired'));
            return;
        }

        // The control-record gates the engine applies (PayrollControlRecordGuard), told before the run is queued
        // rather than by a failed job. Simulation runs are left to the simulation engine to judge.
        const context = this.context();
        if (!this.isSimulation() && context?.hasControlRecord) {
            if (!this.isOffCycle() && !context.regularRunAllowed) {
                this.message.warn(this.l('PayrollRunRegularNeedsLockedPeriod'));
                return;
            }

            if (this.isOffCycle() && !context.offCycleRunAllowed) {
                this.message.warn(this.l('PayrollRunOffCycleNeedsClosedPeriod'));
                return;
            }

            if (this.isOffCycle() && context.showOffCyclePaymentDate && !this.offCyclePaymentDateInRange()) {
                this.message.warn(
                    this.l(
                        'PayrollRunOffCyclePaymentDateOutOfPeriod',
                        this.formatDate(context.offCyclePaymentDateFrom),
                        this.formatDate(context.offCyclePaymentDateTo)
                    )
                );
                return;
            }
        }

        const codes = this.splitCodes(this.employeeCodes());
        if (codes.length) {
            this.confirmRun(this.l('PayrollRunEmployeesCount', codes.length));
            return;
        }

        // No codes = every eligible employee of the area; say how many before asking.
        this.running.set(true);
        this._payrollRunSetupService
            .getPayrollRunCandidates(
                new GetPayrollRunCandidatesInput({
                    payrollAreaName: this.payrollAreaName(),
                    payrollRunType: +this.payrollRunType() as PayrollRunType,
                    offCycleReason: this.isOffCycle() ? this.offCycleReason() : undefined,
                    statusFilter: PayrollRunCandidateFilter.All,
                    filter: undefined,
                    selectedEmployeeCodes: undefined,
                    simulationMonth: this.simulatingOtherPeriod() ? this.simulationMonth() : undefined,
                    simulationYear: this.simulatingOtherPeriod() ? this.simulationYear() : undefined,
                    sorting: undefined,
                    skipCount: 0,
                    maxResultCount: 1,
                })
            )
            .pipe(finalize(() => this.running.set(false)))
            .subscribe((result) => this.confirmRun(this.l('PayrollRunAllEmployeesCount', result.allCount)));
    }

    /** Legacy ran on a click; this asks first, with area, period, run type and how many employees. */
    private confirmRun(employeesText: string): void {
        const runTypeText =
            (this.isOffCycle() ? this.l('OffCycle') + ' - ' + this.selectedReasonName() : this.l('Regular')) +
            (this.isSimulation() ? ' (' + this.l('PayrollRunSimulation') + ')' : '');

        this.message.confirm(
            this.l('PayrollRunConfirmMessage', this.payrollAreaName(), this.periodText(), runTypeText, employeesText),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this.queueRun();
                }
            }
        );
    }

    private queueRun(): void {
        const simulatingOther = this.simulatingOtherPeriod();

        const input = new RunPayrollEngineInput({
            payrollRunType: +this.payrollRunType() as PayrollRunType,
            employeeCodes: this.employeeCodes() ? this.employeeCodes() : undefined,
            payInDate: DateTime.fromISO(this.payInDate()),
            payrollAreaName: this.payrollAreaName(),
            offCyclePaymentDate:
                this.showOffCyclePaymentDate() && this.offCyclePaymentDate()
                    ? DateTime.fromISO(this.offCyclePaymentDate())
                    : undefined,
            offCycleReason: this.isOffCycle() && this.offCycleReason() ? this.offCycleReason() : undefined,
            isSimulation: this.isSimulation(),
            simulationOtherPeriod: simulatingOther,
            simulationMonth: simulatingOther ? this.simulationMonth() : undefined,
            simulationYear: simulatingOther ? this.simulationYear() : undefined,
        });

        this.running.set(true);
        this._payrollEngineService
            .runPayrollEngine(input)
            .pipe(finalize(() => this.running.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('PayrollEngineRunQueued'));
                this.reloadPage();
                this.engineProgress()?.onRunQueued();
                this.startPolling();
            });
    }

    private offCyclePaymentDateInRange(): boolean {
        const context = this.context();
        if (!this.offCyclePaymentDate() || !context?.offCyclePaymentDateFrom || !context?.offCyclePaymentDateTo) {
            // A missing date is the engine's to report ("offCycle PaymentDate not entered.").
            return true;
        }

        const date = this.offCyclePaymentDate();
        return (
            date >= this.toDateInput(context.offCyclePaymentDateFrom) &&
            date <= this.toDateInput(context.offCyclePaymentDateTo)
        );
    }

    private selectedReasonName(): string {
        const reason = this.offCycleReasons().find((r) => r.value === this.offCycleReason());
        return reason ? reason.displayName : this.offCycleReason();
    }

    private periodText(): string {
        if (this.simulatingOtherPeriod() && this.simulationMonth() && this.simulationYear()) {
            return this.simulationMonth() + '/' + this.simulationYear();
        }

        const context = this.context();
        if (context?.hasControlRecord) {
            return context.periodStatusText || context.payrollMonth + '/' + context.payrollYear;
        }

        return this.payInDate();
    }

    private toDateInput(value: DateTime | undefined): string {
        return value ? value.toFormat('yyyy-MM-dd') : '';
    }

    private formatDate(value: DateTime | undefined): string {
        return value ? value.toFormat('dd/MM/yyyy') : '';
    }

    private splitCodes(codes: string): string[] {
        return (codes || '')
            .split(',')
            .map((c) => c.trim())
            .filter((c) => c.length > 0);
    }

    // --------------------------------------------------------------------------------------------------------------
    // Run history
    // --------------------------------------------------------------------------------------------------------------

    getExecutionLogs(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._executionLogsService
            .getAll(
                new GetAllExecutionLogsInput({
                    module: ExecutionLogModule.PayrollEngine,
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.latestLog.set(result.items && result.items.length ? result.items[0] : undefined);

                const inProgress = (result.items || []).some(
                    (item) => item.executionStatus === ExecutionLogStatus.INPRECESS
                );
                if (!inProgress) {
                    this.stopPolling();
                }

                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getExecutionLogs({ first: (this.paginator()?.getPage() ?? 0) * rows, rows } as LazyLoadEvent);
    }

    executionStatusText(status: ExecutionLogStatus): string {
        return ExecutionLogStatus[status];
    }

    private startPolling(): void {
        if (this._pollHandle) {
            return;
        }
        this._pollHandle = setInterval(() => this.reloadPage(), 10000);
    }

    private stopPolling(): void {
        if (this._pollHandle) {
            clearInterval(this._pollHandle);
            this._pollHandle = undefined;
        }
    }
}
