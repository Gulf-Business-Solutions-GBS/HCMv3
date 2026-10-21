import { Directive, OnDestroy, OnInit, computed, signal, viewChild } from '@angular/core';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';

import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CostingProcessStatusDto,
    CostingRunInputDto,
    JournalVoucherLineDto,
    PayrollAreaLookupDto,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';
import { CostingEmployeePickerModalComponent } from './costing-employee-picker-modal.component';
import { FinanceRunLogModalComponent } from './finance-run-log-modal.component';
import { FinanceScreenApi } from './finance-screen-api';

/**
 * What the Costing File and Post JV screens have in common -- in legacy they were one screen
 * (uscCostingFile.ascx):
 *
 * - Period "Current" / "Other" (legacy rdCurrent / rdOtherPeriod). Current fills month, year, start and end from
 *   the payroll area's control record (SetCurrentDates, lines 1510-1525); Other works them out for a typed month
 *   and year (SetDates, lines 1499-1508). The start and end dates are read-only, as legacy's were, and the end date
 *   is what is sent as the pay-in date.
 * - Posting date defaulted to the period end when empty, as legacy did on both its D365 and AX screens.
 * - The employee "..." popup, the live status panel with Stop and Clear Status, and "Show details".
 * - The JV preview / Validate JV.
 *
 * Long runs are background jobs on the server; the status is polled while one is going (legacy's timer1).
 */
@Directive()
export abstract class FinanceScreenBase extends AppComponentBase implements OnInit, OnDestroy {
    /** Legacy timer interval for the status panel. */
    static readonly PollMilliseconds = 3000;

    protected abstract readonly api: FinanceScreenApi;

    readonly logModal = viewChild<FinanceRunLogModalComponent>('logModal');
    readonly pickerModal = viewChild<CostingEmployeePickerModalComponent>('pickerModal');

    readonly PayrollRunType = PayrollRunType;

    readonly payrollAreas = signal<PayrollAreaLookupDto[]>([]);
    readonly payrollAreaName = signal<string | undefined>(undefined);
    readonly payrollRunType = signal<PayrollRunType>(PayrollRunType.Regular);
    readonly employeeCode = signal<string>('');

    readonly otherPeriod = signal<boolean>(false);
    readonly payrollMonth = signal<number | undefined>(undefined);
    readonly payrollYear = signal<number | undefined>(undefined);
    readonly periodStart = signal<DateTime | undefined>(undefined);
    readonly periodEnd = signal<DateTime | undefined>(undefined);
    readonly periodMessage = signal<string>('');

    /** yyyy-MM-dd, bound to a native date input. */
    readonly postingDate = signal<string>('');

    readonly lines = signal<JournalVoucherLineDto[]>([]);
    readonly totalDebit = signal<number>(0);
    readonly totalCredit = signal<number>(0);
    readonly isBalanced = signal<boolean>(true);
    readonly previewFingerprint = signal<string>('');

    readonly status = signal<CostingProcessStatusDto | undefined>(undefined);
    readonly busy = signal<boolean>(false);

    readonly isRunning = computed(() => !!this.status()?.isRunning);

    /** Legacy blocks the run when the payroll area has no FICompany or FIBank. */
    readonly selectedAreaIsFiConfigured = computed(() => {
        const area = this.payrollAreas().find((a) => a.name === this.payrollAreaName());
        return !!area && area.isFiConfigured;
    });

    readonly canSubmit = computed(
        () => !this.busy() && !!this.payrollAreaName() && !!this.periodEnd() && !!this.postingDate()
    );

    readonly canPickEmployees = computed(() => !!this.payrollAreaName() && !!this.periodEnd());

    private _pollHandle: any;

    ngOnInit(): void {
        this.api.getPayrollAreasForLookup().subscribe((areas) => this.payrollAreas.set(areas));
        this.refreshStatus();
    }

    ngOnDestroy(): void {
        this.stopPolling();
    }

    // ------------------------------------------------------------------ inputs

    onPayrollAreaChange(name: string | undefined): void {
        this.payrollAreaName.set(name);
        this.invalidatePreview();
        this.loadPeriod();
    }

    onPayrollRunTypeChange(runType: PayrollRunType): void {
        this.payrollRunType.set(runType);
        this.invalidatePreview();
    }

    onEmployeeCodeChange(codes: string): void {
        this.employeeCode.set(codes);
        this.invalidatePreview();
    }

    onPostingDateChange(value: string): void {
        this.postingDate.set(value || '');
        this.invalidatePreview();
    }

    onPeriodModeChange(other: boolean): void {
        this.otherPeriod.set(other);
        this.invalidatePreview();
        this.loadPeriod();
    }

    onMonthChange(value: any): void {
        this.payrollMonth.set(value ? +value : undefined);
        this.invalidatePreview();
        this.loadOtherPeriod();
    }

    onYearChange(value: any): void {
        this.payrollYear.set(value ? +value : undefined);
        this.invalidatePreview();
        this.loadOtherPeriod();
    }

    private loadPeriod(): void {
        if (this.otherPeriod()) {
            this.loadOtherPeriod();
        } else {
            this.loadCurrentPeriod();
        }
    }

    /** Legacy SetCurrentDates. */
    private loadCurrentPeriod(): void {
        const name = this.payrollAreaName();
        this.periodStart.set(undefined);
        this.periodEnd.set(undefined);
        this.periodMessage.set('');

        if (!name) {
            return;
        }

        this.api.getCostingPeriodContext(name).subscribe((context) => {
            if (this.payrollAreaName() !== name || this.otherPeriod()) {
                return;
            }

            if (!context.hasControlRecord) {
                this.periodMessage.set(context.message || '');
                return;
            }

            this.payrollMonth.set(context.payrollMonth);
            this.payrollYear.set(context.payrollYear);
            this.periodStart.set(context.calendarMonthStart);
            this.periodEnd.set(context.calendarMonthEnd);

            // Legacy: "if (txtPostingDate.Text == "") txtPostingDate.Text = rec.CalendarMonthEnd".
            if (!this.postingDate() && context.defaultPostingDate) {
                this.postingDate.set(context.defaultPostingDate.toFormat('yyyy-MM-dd'));
            }
        });
    }

    /** Legacy SetDates. */
    private loadOtherPeriod(): void {
        const name = this.payrollAreaName();
        const month = this.payrollMonth();
        const year = this.payrollYear();

        this.periodStart.set(undefined);
        this.periodEnd.set(undefined);
        this.periodMessage.set('');

        if (!name || !month || !year || month < 1 || month > 12 || year < 1900) {
            return;
        }

        this.api.getCostingPeriodDates(name, month, year).subscribe((dates) => {
            if (this.payrollAreaName() !== name || this.payrollMonth() !== month || this.payrollYear() !== year) {
                return;
            }

            this.periodStart.set(dates.calendarMonthStart);
            this.periodEnd.set(dates.calendarMonthEnd);
        });
    }

    protected buildInput(): CostingRunInputDto {
        return new CostingRunInputDto({
            payrollAreaName: this.payrollAreaName(),
            employeeCode: this.employeeCode(),
            payInDate: DateTime.fromISO(this.periodEnd().toFormat('yyyy-MM-dd')),
            postingDate: DateTime.fromISO(this.postingDate()),
            payrollRunType: this.payrollRunType(),
            previewFingerprint: undefined,
        });
    }

    /** Anything that changes the JV makes the shown preview stale, so it can no longer be posted. */
    protected invalidatePreview(): void {
        this.lines.set([]);
        this.previewFingerprint.set('');
    }

    // ------------------------------------------------------------------ employee popup

    openEmployeePicker(): void {
        if (!this.canPickEmployees()) {
            return;
        }

        this.pickerModal().show({
            api: this.api,
            payrollAreaName: this.payrollAreaName(),
            payrollRunType: this.payrollRunType(),
            payInDate: this.periodEnd(),
            selectedCodes: this.employeeCode(),
        });
    }

    onCodesSelected(codes: string): void {
        this.onEmployeeCodeChange(codes);
    }

    // ------------------------------------------------------------------ JV preview

    previewJournalVoucher(): void {
        if (!this.canSubmit()) {
            return;
        }

        this.busy.set(true);
        this.api
            .getJournalVoucherLines(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => {
                this.lines.set(result.lines ?? []);
                this.totalDebit.set(result.totalDebit);
                this.totalCredit.set(result.totalCredit);
                this.isBalanced.set(result.isBalanced);
                this.previewFingerprint.set(result.previewFingerprint || '');
            });
    }

    /** Legacy "Validate JV" -> ExportGrid(onlyValidate: true). */
    validateJournalVoucher(): void {
        if (!this.canSubmit()) {
            return;
        }

        this.busy.set(true);
        this.api
            .validateJournalVoucher(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => {
                this.totalDebit.set(result.totalDebit);
                this.totalCredit.set(result.totalCredit);
                this.isBalanced.set(result.isValid);

                if (result.isValid) {
                    this.notify.success(result.message);
                } else {
                    this.message.error(result.message);
                }
            });
    }

    // ------------------------------------------------------------------ status panel

    refreshStatus(): void {
        this.api.getProcessStatus().subscribe((status) => {
            this.status.set(status);

            if (status.isRunning) {
                this.startPolling();
            } else {
                this.stopPolling();
            }
        });
    }

    protected onRunQueued(message: string): void {
        this.notify.info(message);
        this.refreshStatus();
        this.startPolling();
    }

    /** Legacy Stop button. */
    requestStop(): void {
        this.api.requestStop().subscribe(() => {
            this.notify.info(this.l('EngineRunStopRequested'));
            this.refreshStatus();
        });
    }

    /** Legacy "Clear Status" -> TruncateLog. */
    clearStatus(): void {
        this.busy.set(true);
        this.api
            .clearProcessStatus()
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('EngineRunProgressCleared'));
                this.refreshStatus();
            });
    }

    showDetails(): void {
        this.logModal().show(this.api);
    }

    private startPolling(): void {
        if (this._pollHandle) {
            return;
        }

        this._pollHandle = setInterval(() => this.refreshStatus(), FinanceScreenBase.PollMilliseconds);
    }

    private stopPolling(): void {
        if (this._pollHandle) {
            clearInterval(this._pollHandle);
            this._pollHandle = undefined;
        }
    }
}
