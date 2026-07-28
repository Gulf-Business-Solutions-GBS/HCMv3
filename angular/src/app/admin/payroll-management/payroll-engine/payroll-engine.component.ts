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
    ExecutionLogDto,
    ExecutionLogModule,
    ExecutionLogStatus,
    ExecutionLogsServiceProxy,
    GetAllExecutionLogsInput,
    GetAllPayrollAreasInput,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    PayrollEngineServiceProxy,
    PayrollRunType,
    RunPayrollEngineInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

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
 * Deliberately follows TimeEngineComponent one-for-one so both engine screens behave identically.
 */
@Component({
    templateUrl: './payroll-engine.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        TableModule,
        PaginatorModule,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
    ],
})
export class PayrollEngineComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _payrollEngineService = inject(PayrollEngineServiceProxy);
    private _executionLogsService = inject(ExecutionLogsServiceProxy);
    private _payrollAreasService = inject(PayrollAreasServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

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

    // Mirrors HCMv3.Core PayrollOffCycleReasons -- the engine matches these literals BY VALUE, so the
    // dropdown must emit exactly these strings (see PayrollEngineDtos.cs OffCycleReason remarks).
    readonly offCycleReasons: { value: string; localizationKey: string }[] = [
        { value: 'End Of Service', localizationKey: 'EndOfService' },
        { value: 'Advance Leave', localizationKey: 'AdvanceLeave' },
        { value: 'Leave Encashment', localizationKey: 'LeaveEncashment' },
    ];

    readonly PayrollRunType = PayrollRunType;

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

    isOffCycle(): boolean {
        return +this.payrollRunType() === PayrollRunType.OffCycle;
    }

    onPayrollRunTypeChange(value: any): void {
        this.payrollRunType.set(+value as PayrollRunType);
        if (!this.isOffCycle()) {
            this.offCycleReason.set('');
            this.offCyclePaymentDate.set('');
        }
    }

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

        const input = new RunPayrollEngineInput({
            payrollRunType: +this.payrollRunType() as PayrollRunType,
            employeeCodes: this.employeeCodes() ? this.employeeCodes() : undefined,
            payInDate: DateTime.fromISO(this.payInDate()),
            payrollAreaName: this.payrollAreaName(),
            offCyclePaymentDate: this.offCyclePaymentDate()
                ? DateTime.fromISO(this.offCyclePaymentDate())
                : undefined,
            offCycleReason: this.offCycleReason() ? this.offCycleReason() : undefined,
        });

        this.running.set(true);
        this._payrollEngineService
            .runPayrollEngine(input)
            .pipe(finalize(() => this.running.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('PayrollEngineRunQueued'));
                this.reloadPage();
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
