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
    GetAllPayrollAreasInput,
    GetAllProjectLogsInput,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    PayrollRunType,
    ProjectCostEngineServiceProxy,
    ProjectCostEngineStatusDto,
    ProjectLogsServiceProxy,
    RunProjectCostEngineInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

/**
 * Port of the legacy Project Cost Engine screen
 * (HCM10.Web/UserControls/ProjectManagement/uscProjectCosting.ascx + .ascx.cs).
 *
 * uscProjectCosting2.* is a rename-only clone of the same screen and is intentionally NOT ported separately.
 *
 * Legacy ran the engine on a ThreadPool work item inside the web process and polled a DevExpress callback
 * panel driven by an ASP.NET timer. Here the run is enqueued as an ABP background job by
 * IProjectCostEngineAppService.RunProjectCostEngine, and progress is read from
 * IProjectCostEngineAppService.GetProjectCostEngineStatus (the port of legacy CalculateProcessStatus) plus
 * the ProjectLog rows the engine writes.
 *
 * Deliberately follows PayrollEngineComponent one-for-one so all three engine screens behave identically.
 */
@Component({
    templateUrl: './project-cost-engine.component.html',
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
export class ProjectCostEngineComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _projectCostEngineService = inject(ProjectCostEngineServiceProxy);
    private _projectLogsService = inject(ProjectLogsServiceProxy);
    private _payrollAreasService = inject(PayrollAreasServiceProxy);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    // Legacy form fields -- see uscProjectCosting.ascx.cs ExecuteLongOperation() lines 128-155.
    readonly payrollRunType = signal<PayrollRunType>(PayrollRunType.Regular);
    readonly employeeCodes = signal<string>('');
    readonly payInDate = signal<string>('');
    readonly payrollAreaName = signal<string>('');
    readonly offCyclePaymentDate = signal<string>('');
    readonly offCycleReason = signal<string>('');

    readonly payrollAreas = signal<PayrollAreaDto[]>([]);
    readonly running = signal<boolean>(false);
    readonly status = signal<ProjectCostEngineStatusDto | undefined>(undefined);

    // Legacy BindOffReasonType (lines 1342-1358) offered exactly these two. The engine compares the reason
    // BY VALUE against the literal "End Of Service" (legacy line 152), so the values must match exactly.
    readonly offCycleReasons: { value: string; localizationKey: string }[] = [
        { value: 'Advance Leave', localizationKey: 'AdvanceLeave' },
        { value: 'End Of Service', localizationKey: 'EndOfService' },
    ];

    readonly PayrollRunType = PayrollRunType;

    private _pollHandle: any;

    ngOnInit(): void {
        // The PrimeNG table's (onLazyLoad) fires the first getProjectLogs() -- do not call it here.
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

        this.refreshStatus();
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

    /** Legacy btnCalculateProjectCosting (lines 1604-1613). */
    runProjectCostEngine(): void {
        // Same order the app service validates in, so the client surfaces the same message.
        if (!this.payrollAreaName() && !this.employeeCodes()) {
            this.message.warn(this.l('ProjectCostEnginePayrollAreaOrEmployeeCodeRequired'));
            return;
        }

        if (!this.payInDate()) {
            this.message.warn(this.l('ProjectCostEnginePayInDateRequired'));
            return;
        }

        if (!this.payrollAreaName()) {
            this.message.warn(this.l('ProjectCostEnginePayrollAreaRequired'));
            return;
        }

        if (this.isOffCycle() && !this.offCyclePaymentDate()) {
            this.message.warn(this.l('ProjectCostEngineOffCyclePaymentDateRequired'));
            return;
        }

        const input = new RunProjectCostEngineInput({
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
        this._projectCostEngineService
            .runProjectCostEngine(input)
            .pipe(finalize(() => this.running.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('ProjectCostEngineRunQueued'));
                this.reloadPage();
                this.startPolling();
            });
    }

    /** Legacy btnStopProjectCosting (lines 1614-1628). */
    stopProjectCostEngine(): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._projectCostEngineService.stopProjectCostEngine().subscribe(() => {
                this.notify.info(this.l('ProjectCostEngineStopped'));
                this.refreshStatus();
            });
        });
    }

    /** Legacy btnClearStatus (lines 1629-1652). */
    clearStatus(): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._projectCostEngineService.clearProjectCostEngineLog().subscribe(() => {
                this.notify.info(this.l('ProjectCostEngineStatusCleared'));
                this.stopPolling();
                this.refreshStatus();
                this.reloadPage();
            });
        });
    }

    /** Legacy CalculateProcessStatus polled by timer1 (lines 1446-1508). */
    refreshStatus(): void {
        this._projectCostEngineService.getProjectCostEngineStatus().subscribe((result) => {
            this.status.set(result);

            // Legacy stopped its timer once the run finished; here "finished" is "every expected employee
            // has a log row", or a stop was requested.
            if (result.stopRequested || (result.noOfEmployee > 0 && result.totalEmployeesProcessed >= result.noOfEmployee)) {
                this.stopPolling();
            }
        });
    }

    getProjectLogs(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._projectLogsService
            .getAll(
                new GetAllProjectLogsInput({
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    employeeId: undefined,
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

    reloadPage(): void {
        const rows = this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getProjectLogs({ first: (this.paginator()?.getPage() ?? 0) * rows, rows } as LazyLoadEvent);
    }

    /** Legacy grid column "Message" was paired with a Status of 0 (success) / 1 (failed). */
    statusText(status: number): string {
        return status === 0 ? this.l('Successfull') : this.l('Failed');
    }

    private startPolling(): void {
        if (this._pollHandle) {
            return;
        }
        this._pollHandle = setInterval(() => {
            this.refreshStatus();
            this.reloadPage();
        }, 10000);
    }

    private stopPolling(): void {
        if (this._pollHandle) {
            clearInterval(this._pollHandle);
            this._pollHandle = undefined;
        }
    }
}
