import { Component, computed, input, output } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { CostingProcessStatusDto, FinanceRunStatus } from '@shared/service-proxies/service-proxies';

/**
 * Legacy status panel of uscCostingFile.ascx (CalculateProcessStatus, lines 2484-2585; status label, lines
 * 2117-2184): the status text, the progress ring, the employee being processed, the estimate, and the All /
 * Successful / Failed counters with "Show details". Shared by the Costing File and Post JV screens.
 */
@Component({
    selector: 'financeRunStatus',
    templateUrl: './finance-run-status.component.html',
    imports: [LocalizePipe, LuxonFormatPipe],
})
export class FinanceRunStatusComponent extends AppComponentBase {
    readonly status = input<CostingProcessStatusDto | undefined>(undefined);

    /** Legacy "Show details": opens the per-employee / per-batch log. */
    readonly showDetails = output<void>();

    readonly FinanceRunStatus = FinanceRunStatus;

    readonly statusKey = computed(() => {
        switch (this.status()?.runStatus) {
            case FinanceRunStatus.Running:
                return 'EngineRunStatus_Running';
            case FinanceRunStatus.Completed:
                return 'EngineRunStatus_Completed';
            case FinanceRunStatus.CompletedWithErrors:
                return 'EngineRunStatus_CompletedWithErrors';
            case FinanceRunStatus.Stopped:
                return 'EngineRunStatus_Aborted';
            case FinanceRunStatus.Interrupted:
                return 'EngineRunInterrupted';
            default:
                return '';
        }
    });

    readonly statusClass = computed(() => {
        switch (this.status()?.runStatus) {
            case FinanceRunStatus.Running:
            case FinanceRunStatus.Completed:
            case FinanceRunStatus.Stopped:
                return 'badge badge-light-success fs-6';
            case FinanceRunStatus.CompletedWithErrors:
            case FinanceRunStatus.Interrupted:
                return 'badge badge-light-danger fs-6';
            default:
                return 'badge badge-light fs-6';
        }
    });

    readonly progress = computed(() => Math.max(0, Math.min(100, this.status()?.progressPercent ?? 0)));

    /** Legacy's "hh : mm" estimate. */
    readonly remainingText = computed(() => {
        const minutes = this.status()?.remainingMinutes;
        if (minutes === undefined || minutes === null || minutes < 0) {
            return '';
        }

        const hours = Math.floor(minutes / 60);
        const rest = Math.round(minutes % 60);
        return `${hours.toString().padStart(2, '0')}h : ${rest.toString().padStart(2, '0')}m`;
    });
}
