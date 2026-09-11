import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    GoLiveV2RunPayrollInput,
    GoLiveV2RunPayrollOutput,
    GoLiveV2ServiceProxy,
    GoLiveV2StoppedAt,
} from '@shared/service-proxies/service-proxies';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';

interface Attempt {
    at: Date;
    result: GoLiveV2RunPayrollOutput;
}

/**
 * Go-Live Setup V2, built backwards from a working payroll.
 *
 * It starts with the one step a customer actually wants -- Run payroll -- and nothing else. The engine's
 * refusal is shown in its own words, because that wording is what the next step gets built from. Every
 * attempt is kept in a log on the page, so the path from "nothing" to "a payroll that runs" can be shown
 * as it happened.
 *
 * Each field is added only once the engine has asked for it, and says so on screen. The first press sent
 * nothing and was refused for want of a pay date, so the month is the first field; with a month, it asked
 * for a payroll area, so that is the second.
 */
@Component({
    templateUrl: './go-live-v2.component.html',
    styleUrl: './go-live-v2.component.css',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, LocalizePipe, DatePipe],
})
export class GoLiveV2Component extends AppComponentBase {
    private readonly proxy = inject(GoLiveV2ServiceProxy);

    /** 'yyyy-MM' from the month picker; empty until one is chosen. */
    readonly payMonth = signal('');

    /** The last day of the chosen month -- the only pay date the engine accepts. */
    readonly monthEnd = computed(() => {
        const month = this.payMonth();
        return month ? DateTime.fromISO(`${month}-01`).endOf('month').startOf('day') : null;
    });

    /** The payroll area to run, by name -- the engine matches it by name. Pre-filled so one press is enough. */
    readonly payrollAreaName = signal('Default Payroll Area');

    readonly running = signal(false);
    readonly latest = signal<GoLiveV2RunPayrollOutput | null>(null);
    readonly attempts = signal<Attempt[]>([]);

    readonly StoppedAt = GoLiveV2StoppedAt;

    runPayroll(): void {
        if (this.running()) {
            return;
        }

        this.running.set(true);

        this.proxy
            .runPayroll(this.buildInput())
            .pipe(finalize(() => this.running.set(false)))
            .subscribe((result) => {
                this.latest.set(result);
                this.attempts.update((list) => [{ at: new Date(), result }, ...list]);
            });
    }

    onPayMonthChange(event: Event): void {
        this.payMonth.set((event.target as HTMLInputElement).value);
    }

    onPayrollAreaChange(event: Event): void {
        this.payrollAreaName.set((event.target as HTMLInputElement).value);
    }

    /** Where the run stopped, as a sentence the customer recognises. */
    outcomeKey(result: GoLiveV2RunPayrollOutput): string {
        switch (result.stoppedAt) {
            case GoLiveV2StoppedAt.Input:
                return 'GoLiveV2StoppedAtInput';
            case GoLiveV2StoppedAt.Engine:
                return 'GoLiveV2StoppedAtEngine';
            case GoLiveV2StoppedAt.Employees:
                return 'GoLiveV2StoppedAtEmployees';
            case GoLiveV2StoppedAt.NoEmployees:
                return 'GoLiveV2StoppedAtNoEmployees';
            default:
                return 'GoLiveV2Succeeded';
        }
    }

    payPeriod(result: GoLiveV2RunPayrollOutput): string | null {
        return result.payInDate ? this.l('GoLiveV2PaidUpTo', result.payInDate.toFormat('dd/MM/yyyy')) : null;
    }

    /**
     * Sends midday on the month's last day rather than midnight on its first. A date is serialised with the
     * browser's offset, so Kuwait midnight on the 1st is still the 31st of the previous month on a server
     * running in UTC -- and the payroll would run for the wrong month. Midday on the last day cannot cross
     * a month boundary in any time zone. The server takes the month end regardless.
     */
    private buildInput(): GoLiveV2RunPayrollInput {
        const input = new GoLiveV2RunPayrollInput();
        const end = this.monthEnd();

        if (end) {
            input.payDate = end.plus({ hours: 12 });
        }

        const payrollArea = this.payrollAreaName().trim();
        if (payrollArea) {
            input.payrollAreaName = payrollArea;
        }

        return input;
    }
}
