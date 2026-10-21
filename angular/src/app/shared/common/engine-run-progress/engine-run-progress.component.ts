import { Component, OnDestroy, OnInit, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';

import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    EngineRunEmployeeDto,
    EngineRunStatus,
    EngineRunStatusDto,
    EngineType,
    GetEngineRunEmployeesInput,
    GetTimeEngineRunEmployeesInput,
    PagedResultDtoOfEngineRunEmployeeDto,
    PayrollEngineServiceProxy,
    ProjectCostEngineServiceProxy,
    TimeEngineServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';

/**
 * Operator-facing live progress panel for an engine run.
 *
 * One component serves BOTH the Time Engine and the Payroll Engine: the backend deliberately exposes the
 * identical EngineRunStatusDto / EngineRunEmployeeDto / GetEngineRunEmployeesInput contract for both, so a
 * pair of near-duplicate components would be pure drift risk. The only per-engine differences are which
 * service proxy methods are called and which "*.Run" permission gates the action buttons -- both derived
 * from the [engine] input.
 *
 * Polling follows project-cost-engine.component.ts (itself a port of legacy CalculateProcessStatus on a
 * DevExpress timer): a setInterval torn down in ngOnDestroy AND the moment the run is no longer live. A
 * stale run (heartbeat older than StaleThresholdSeconds) is NOT live -- the backend already reports
 * IsRunning=false for it -- so a crashed run can never leave an interval hammering the server.
 *
 * Contract points from EngineRunStatusDto honoured here rather than glossed over:
 *  - hasRun === false -> "no runs yet" empty state, never a 0% bar.
 *  - totalEmployees === 0 while running -> "counting employees...", never "nothing to do".
 *  - averageSecondsPerEmployee == null -> "estimating...", never a bogus 0.
 *  - estimated* are labelled as estimates and go null once the run ends.
 *  - isStale -> shown as interrupted, with Abandon offered as the recovery action.
 */
@Component({
    selector: 'engine-run-progress',
    templateUrl: './engine-run-progress.component.html',
    imports: [
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
    ],
})
export class EngineRunProgressComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _timeEngineService = inject(TimeEngineServiceProxy);
    private _payrollEngineService = inject(PayrollEngineServiceProxy);
    private _projectCostEngineService = inject(ProjectCostEngineServiceProxy);

    /** Which engine's runs this panel shows. Drives both the endpoints and the permission names. */
    readonly engine = input.required<EngineType>();

    /**
     * Time Engine only: follow the latest run of this payroll area (blank = of any area) -- status, grid and Stop.
     * The Time Engine runs one run per area at a time, so the screen follows the area it has selected. Ignored for
     * the other engines, whose endpoints take no area.
     */
    readonly payrollAreaName = input<string | undefined>(undefined);

    /** Show a "Days" button on each employee row; pressing it emits employeeDaysRequested. Off by default. */
    readonly showEmployeeDays = input<boolean>(false);

    /** The employee row whose day-level log the host screen should show. */
    readonly employeeDaysRequested = output<EngineRunEmployeeDto>();

    readonly dataTable = viewChild<Table>('employeeTable');
    readonly paginator = viewChild<Paginator>('employeePaginator');

    readonly status = signal<EngineRunStatusDto | undefined>(undefined);
    readonly statusLoading = signal<boolean>(false);
    readonly working = signal<boolean>(false);

    readonly filterText = signal<string>('');

    /** true = failures only, false = successes only, undefined = every employee. */
    readonly onlyFailed = signal<boolean | undefined>(undefined);

    readonly EngineRunStatus = EngineRunStatus;

    /** "Pages.TimeManagement.TimeEngine.Run" / "Pages.PayrollManagement.PayrollEngine.Run". */
    readonly runPermission = computed(() =>
        this.engine() === EngineType.TimeEngine
            ? 'Pages.TimeManagement.TimeEngine.Run'
            : this.engine() === EngineType.ProjectCostEngine
              ? 'Pages.ProjectManagement.ProjectCostEngine.Run'
              : 'Pages.PayrollManagement.PayrollEngine.Run'
    );

    private _pollHandle: any;

    ngOnInit(): void {
        // The employee grid's (onLazyLoad) fires the first getRunEmployees() -- do not call it here.
        this.refreshStatus();
    }

    ngOnDestroy(): void {
        this.stopPolling();
    }

    // ------------------------------------------------------------------------------------------------
    // Endpoint dispatch (the two engines share one DTO contract)
    // ------------------------------------------------------------------------------------------------

    private isTimeEngine(): boolean {
        return this.engine() === EngineType.TimeEngine;
    }

    private isProjectCostEngine(): boolean {
        return this.engine() === EngineType.ProjectCostEngine;
    }

    /** The Time Engine area scope, or undefined for "any area". */
    private scopeAreaName(): string | undefined {
        const area = this.payrollAreaName();
        return area ? area : undefined;
    }

    private fetchStatus(): Observable<EngineRunStatusDto> {
        if (this.isProjectCostEngine()) {
            return this._projectCostEngineService.getProjectCostEngineStatus();
        }
        return this.isTimeEngine()
            ? this._timeEngineService.getTimeEngineStatus(this.scopeAreaName())
            : this._payrollEngineService.getPayrollEngineStatus();
    }

    private fetchEmployees(input: GetEngineRunEmployeesInput): Observable<PagedResultDtoOfEngineRunEmployeeDto> {
        if (this.isProjectCostEngine()) {
            return this._projectCostEngineService.getProjectCostEngineRunEmployees(input);
        }
        if (!this.isTimeEngine()) {
            return this._payrollEngineService.getPayrollEngineRunEmployees(input);
        }

        return this._timeEngineService.getTimeEngineRunEmployees(
            new GetTimeEngineRunEmployeesInput({
                runId: input.runId,
                filter: input.filter,
                onlyFailed: input.onlyFailed,
                sorting: input.sorting,
                maxResultCount: input.maxResultCount,
                skipCount: input.skipCount,
                payrollAreaName: this.scopeAreaName(),
            })
        );
    }

    /** Called by the host screen when what the panel follows changed (e.g. another payroll area was selected). */
    reload(): void {
        this.refreshStatus();
        this.reloadEmployees();
    }

    // ------------------------------------------------------------------------------------------------
    // Status + polling
    // ------------------------------------------------------------------------------------------------

    refreshStatus(): void {
        this.statusLoading.set(true);
        this.fetchStatus()
            .pipe(finalize(() => this.statusLoading.set(false)))
            .subscribe((result) => {
                this.status.set(result);

                // isRunning is already false for a stale (dead) run, so this also stops polling a run whose
                // process was killed -- exactly the forgotten-interval bug we must not ship.
                if (result.hasRun && result.isRunning) {
                    this.startPolling();
                } else {
                    this.stopPolling();
                }
            });
    }

    /** Called by the host screen right after it queues a run, so progress starts moving immediately. */
    onRunQueued(): void {
        this.refreshStatus();
        this.startPolling();
        this.reloadEmployees();
    }

    private startPolling(): void {
        if (this._pollHandle) {
            return;
        }
        this._pollHandle = setInterval(() => {
            this.refreshStatus();
            this.reloadEmployees();
        }, 5000);
    }

    private stopPolling(): void {
        if (this._pollHandle) {
            clearInterval(this._pollHandle);
            this._pollHandle = undefined;
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Actions
    // ------------------------------------------------------------------------------------------------

    stopEngine(): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.working.set(true);
            const call = this.isProjectCostEngine()
                ? this._projectCostEngineService.stopProjectCostEngine()
                : this.isTimeEngine()
                  ? this._timeEngineService.stopTimeEngine(this.scopeAreaName())
                  : this._payrollEngineService.stopPayrollEngine();

            call.pipe(finalize(() => this.working.set(false))).subscribe(() => {
                this.notify.info(this.l('EngineRunStopRequested'));
                this.refreshStatus();
            });
        });
    }

    clearProgress(): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.working.set(true);
            const call = this.isProjectCostEngine()
                ? this._projectCostEngineService.clearProjectCostEngineProgress()
                : this.isTimeEngine()
                  ? this._timeEngineService.clearTimeEngineProgress()
                  : this._payrollEngineService.clearPayrollEngineProgress();

            call.pipe(finalize(() => this.working.set(false))).subscribe(() => {
                this.notify.info(this.l('EngineRunProgressCleared'));
                this.stopPolling();
                this.refreshStatus();
                this.reloadEmployees();
            });
        });
    }

    /** Recovery action for a stale run: mark dead runs Abandoned so nothing sits "in progress" forever. */
    abandonStaleRuns(): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.working.set(true);
            const call = this.isProjectCostEngine()
                ? this._projectCostEngineService.abandonStaleProjectCostEngineRuns()
                : this.isTimeEngine()
                  ? this._timeEngineService.abandonStaleTimeEngineRuns()
                  : this._payrollEngineService.abandonStalePayrollEngineRuns();

            call.pipe(finalize(() => this.working.set(false))).subscribe((count) => {
                this.notify.info(this.l('EngineRunStaleRunsAbandoned', count));
                this.stopPolling();
                this.refreshStatus();
                this.reloadEmployees();
            });
        });
    }

    // ------------------------------------------------------------------------------------------------
    // Per-employee grid
    // ------------------------------------------------------------------------------------------------

    getRunEmployees(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this.fetchEmployees(
            new GetEngineRunEmployeesInput({
                // Leaving runId null follows the LATEST run, which is what a live progress screen wants.
                runId: undefined,
                filter: this.filterText() ? this.filterText() : undefined,
                onlyFailed: this.onlyFailed(),
                sorting: this.primengTableHelper.getSorting(this.dataTable()),
                maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
            })
        )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadEmployees(): void {
        const rows = this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRunEmployees({ first: (this.paginator()?.getPage() ?? 0) * rows, rows } as LazyLoadEvent);
    }

    applyFilter(): void {
        // A filter change must restart at page 1, otherwise the operator lands on an empty page.
        this.getRunEmployees({
            first: 0,
            rows: this.primengTableHelper.defaultRecordsCountPerPage,
        } as LazyLoadEvent);
    }

    setOnlyFailed(value: boolean | undefined): void {
        this.onlyFailed.set(value);
        this.applyFilter();
    }

    // ------------------------------------------------------------------------------------------------
    // Display helpers
    // ------------------------------------------------------------------------------------------------

    statusText(status: EngineRunStatus): string {
        switch (status) {
            case EngineRunStatus.Running:
                return this.l('EngineRunStatus_Running');
            case EngineRunStatus.Completed:
                return this.l('EngineRunStatus_Completed');
            case EngineRunStatus.CompletedWithErrors:
                return this.l('EngineRunStatus_CompletedWithErrors');
            case EngineRunStatus.Failed:
                return this.l('EngineRunStatus_Failed');
            case EngineRunStatus.Aborted:
                return this.l('EngineRunStatus_Aborted');
            case EngineRunStatus.Abandoned:
                return this.l('EngineRunStatus_Abandoned');
            default:
                return '';
        }
    }

    /** Headline label: stale beats the persisted status, because a stale run is a lie until finalised. */
    headlineStatusText(): string {
        const current = this.status();
        if (!current || !current.hasRun) {
            return '';
        }
        if (current.isStale) {
            return this.l('EngineRunInterrupted');
        }
        return this.statusText(current.status);
    }

    /** Bootstrap contextual class for the run status badge and the progress bar fill. */
    statusCssClass(): string {
        const current = this.status();
        if (!current || !current.hasRun) {
            return 'bg-secondary';
        }
        if (current.isStale) {
            return 'bg-warning';
        }
        if (current.isRunning) {
            return 'bg-primary';
        }
        switch (current.status) {
            case EngineRunStatus.Completed:
                return 'bg-success';
            case EngineRunStatus.CompletedWithErrors:
                return 'bg-warning';
            case EngineRunStatus.Failed:
            case EngineRunStatus.Abandoned:
                return 'bg-danger';
            default:
                return 'bg-secondary';
        }
    }

    /** "Still counting" is a real state: TotalEmployees is 0 only until employee selection finishes. */
    isCounting(): boolean {
        const current = this.status();
        return !!current && current.hasRun && current.isRunning && current.totalEmployees === 0;
    }

    /** 1h 05m 12s / 5m 12s / 12.3s -- never a raw float of seconds in front of an operator. */
    formatDuration(totalSeconds: number | undefined): string {
        if (totalSeconds === undefined || totalSeconds === null || isNaN(totalSeconds)) {
            return '-';
        }

        if (totalSeconds < 60) {
            return (Math.round(totalSeconds * 10) / 10).toFixed(1) + 's';
        }

        const whole = Math.floor(totalSeconds);
        const hours = Math.floor(whole / 3600);
        const minutes = Math.floor((whole % 3600) / 60);
        const seconds = whole % 60;

        if (hours > 0) {
            return hours + 'h ' + this.pad(minutes) + 'm ' + this.pad(seconds) + 's';
        }
        return minutes + 'm ' + this.pad(seconds) + 's';
    }

    private pad(value: number): string {
        return value < 10 ? '0' + value : '' + value;
    }
}
