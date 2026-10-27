import { Component, inject, signal, viewChild } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';

import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EngineRunEmployeeDto,
    GetTimeEngineRunDaysInput,
    TimeEngineRunDayDto,
    TimeEngineServiceProxy,
} from '@shared/service-proxies/service-proxies';

/**
 * One employee's days in one Time Engine run -- the day-level view of legacy's Time Engine log
 * (uscTimeEngine.ascx popupPayrollLog / SetLogGridDataSource: one row per employee per day with its status and
 * message, filtered All / Successful / Failed). Opened from the per-employee grid's "Days" button, so an employee
 * reported failed because of a failed day shows which days and why.
 */
@Component({
    selector: 'timeEngineRunDaysModal',
    templateUrl: './time-engine-run-days-modal.component.html',
    imports: [AppBsModalDirective, LocalizePipe, BusyIfDirective, LuxonFormatPipe],
})
export class TimeEngineRunDaysModalComponent extends AppComponentBase {
    private _timeEngineService = inject(TimeEngineServiceProxy);

    readonly modal = viewChild<ModalDirective>('daysModal');

    readonly active = signal<boolean>(false);
    readonly loading = signal<boolean>(false);
    readonly employee = signal<EngineRunEmployeeDto | undefined>(undefined);
    readonly days = signal<TimeEngineRunDayDto[]>([]);

    /** true = failed days only, false = successful days only, undefined = every day (legacy All / Successful / Failed). */
    readonly onlyFailed = signal<boolean | undefined>(undefined);

    private _runId: number | undefined;

    /**
     * @param runId the run the grid is showing.
     * @param employee the grid row.
     */
    show(runId: number, employee: EngineRunEmployeeDto): void {
        this._runId = runId;
        this.employee.set(employee);
        // A failed employee opens on its failed days: that is what the operator came for.
        this.onlyFailed.set(employee.succeeded ? undefined : true);
        this.days.set([]);
        this.active.set(true);
        this.modal().show();
        this.load();
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    setOnlyFailed(value: boolean | undefined): void {
        this.onlyFailed.set(value);
        this.load();
    }

    private load(): void {
        const employee = this.employee();
        if (!this._runId || !employee || !employee.employeeId) {
            return;
        }

        this.loading.set(true);
        this._timeEngineService
            .getTimeEngineRunDays(
                new GetTimeEngineRunDaysInput({
                    runId: this._runId,
                    employeeId: employee.employeeId,
                    onlyFailed: this.onlyFailed(),
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.days.set(result || []));
    }
}
