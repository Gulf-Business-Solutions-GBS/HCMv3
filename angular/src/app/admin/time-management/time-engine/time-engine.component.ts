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
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    RunTimeEngineInput,
    TimeEngineServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { EngineRunProgressComponent } from '@app/shared/common/engine-run-progress/engine-run-progress.component';

/**
 * Port of the legacy Time Engine run screen
 * (HCM10.Web/UserControls/PayrollManagement/uscTimeEngine.ascx + .ascx.cs).
 *
 * Legacy shelled out to HCM10.TimeEngine/Program.Main on a ThreadPool work item and polled a
 * DevExpress timer for progress. Here the run is enqueued as an ABP background job by
 * ITimeEngineAppService.RunTimeEngine, and progress is read from the ExecutionLog rows the
 * engine publishes (ExecutionLogModule.TimeEngine).
 */
@Component({
    templateUrl: './time-engine.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        EngineRunProgressComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
    ],
})
export class TimeEngineComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _timeEngineService = inject(TimeEngineServiceProxy);
    private _executionLogsService = inject(ExecutionLogsServiceProxy);
    private _payrollAreasService = inject(PayrollAreasServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** The shared live-progress panel; told to start polling as soon as a run is queued. */
    readonly engineProgress = viewChild<EngineRunProgressComponent>('engineProgress');

    readonly EngineType = EngineType;

    // Legacy args[0..3] -- see uscTimeEngine.ascx.cs ExecuteLongOperation().
    readonly employeeCodes = signal<string>('');
    readonly startDate = signal<string>('');
    readonly endDate = signal<string>('');
    readonly payrollAreaName = signal<string>('');

    readonly payrollAreas = signal<PayrollAreaDto[]>([]);
    readonly running = signal<boolean>(false);
    readonly latestLog = signal<ExecutionLogDto | undefined>(undefined);

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
    }

    ngOnDestroy(): void {
        this.stopPolling();
    }

    runTimeEngine(): void {
        if (!this.endDate()) {
            this.message.warn(this.l('TimeEngineEndDateRequired'));
            return;
        }

        if (this.startDate() && this.startDate() > this.endDate()) {
            this.message.warn(this.l('TimeEngineStartDateAfterEndDate'));
            return;
        }

        const input = new RunTimeEngineInput({
            employeeCodes: this.employeeCodes() ? this.employeeCodes() : undefined,
            startDate: this.startDate() ? DateTime.fromISO(this.startDate()) : undefined,
            endDate: DateTime.fromISO(this.endDate()),
            payrollAreaName: this.payrollAreaName() ? this.payrollAreaName() : undefined,
            employeeSubGroupIds: undefined,
        });

        this.running.set(true);
        this._timeEngineService
            .runTimeEngine(input)
            .pipe(finalize(() => this.running.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('TimeEngineRunQueued'));
                this.reloadPage();
                this.engineProgress()?.onRunQueued();
                this.startPolling();
            });
    }

    getExecutionLogs(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._executionLogsService
            .getAll(
                new GetAllExecutionLogsInput({
                    module: ExecutionLogModule.TimeEngine,
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

    statusText(status: ExecutionLogStatus): string {
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
