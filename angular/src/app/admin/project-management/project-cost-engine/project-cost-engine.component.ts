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
    GetAllPayrollAreasInput,
    GetProjectCostEngineCandidatesInput,
    GetProjectCostEngineLogsInput,
    GetProjectCostEngineLogsToExcelInput,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    PayrollRunCandidateFilter,
    PayrollRunType,
    ProjectCostEnginePeriodDto,
    ProjectCostEngineServiceProxy,
    RunProjectCostEngineInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { EngineRunProgressComponent } from '@app/shared/common/engine-run-progress/engine-run-progress.component';
import { ProjectCostEngineEmployeePickerModalComponent } from './project-cost-engine-employee-picker-modal.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Port of the legacy Project Cost Engine screen as reworked in July-August 2026
 * (HCM10.Web/UserControls/ProjectManagement/uscProjectCosting.ascx + .ascx.cs). uscProjectCosting2.* is an older
 * copy and is intentionally not ported.
 *
 * - Payroll Area, Employee Code with the employee picker (legacy popupEmployees).
 * - Period: Current (the control record, legacy SetCurrentDates) or Other (typed month / year through the area's
 *   payroll year, legacy SetDates), with READ-ONLY start / end dates. The end date is what the run is sent with.
 * - Run type and the off-cycle fields are NOT shown: legacy hid them (Visible="False") and always ran Regular; the
 *   server refuses an off-cycle run.
 * - Run / Stop / Clear Status (legacy's three buttons). The run is an ABP background job saving ten employees at a
 *   time; progress is the SHARED engine-run panel (status, % bar, stale detection, Abandon, per-employee results),
 *   exactly as the Time and Payroll Engine screens.
 * - The run log (legacy log popup: All / Successful / Failed, Export to Excel). Every run starts with an empty log,
 *   as legacy's Run button truncated it; earlier runs stay in the progress history and the execution log.
 *
 * Everything goes through ProjectCostEngineServiceProxy, so the screen needs only the Project Cost Engine
 * permissions -- not Project Logs, not Payroll Engine.
 */
@Component({
    templateUrl: './project-cost-engine.component.html',
    animations: [appModuleAnimation()],
    imports: [
        HelpPageDirective,
        SubHeaderComponent,
        EngineRunProgressComponent,
        ProjectCostEngineEmployeePickerModalComponent,
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
    private _payrollAreasService = inject(PayrollAreasServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** The shared live-progress panel; told to start polling as soon as a run is queued. */
    readonly engineProgress = viewChild<EngineRunProgressComponent>('engineProgress');
    readonly employeePicker = viewChild<ProjectCostEngineEmployeePickerModalComponent>('employeePicker');

    readonly EngineType = EngineType;

    // Legacy cmbPayrollArea / txtEmpCode.
    readonly payrollAreas = signal<PayrollAreaDto[]>([]);
    readonly payrollAreaName = signal<string>('');
    readonly employeeCodes = signal<string>('');

    // Legacy rdCurrent / rdOtherPeriod / txtMonth / txtyear / txtStartDate / txtEndDate.
    readonly otherPeriod = signal<boolean>(false);
    readonly month = signal<number | undefined>(undefined);
    readonly year = signal<number | undefined>(undefined);
    readonly period = signal<ProjectCostEnginePeriodDto | undefined>(undefined);
    readonly periodLoading = signal<boolean>(false);

    readonly running = signal<boolean>(false);
    readonly exporting = signal<boolean>(false);

    // Legacy rdLogAll / rdLogSuccess / rdLogFailed: undefined = all, true = failed only, false = successful only.
    readonly logOnlyFailed = signal<boolean | undefined>(undefined);
    readonly logAllCount = signal<number>(0);
    readonly logSuccessCount = signal<number>(0);
    readonly logFailedCount = signal<number>(0);

    private _pollHandle: any;
    private _baselineRunId: number | undefined;
    private _waitingForNewRun = false;
    private _waitTicks = 0;

    ngOnInit(): void {
        // The log table's (onLazyLoad) fires the first getLogs() -- do not call it here.
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

        // A run may already be going (started earlier or by someone else): keep the log fresh until it ends.
        this.startLogPolling(false);
    }

    ngOnDestroy(): void {
        this.stopLogPolling();
    }

    // --------------------------------------------------------------------------------------------------------------
    // Period (legacy ASPxCallbackPanelPeriod: SetCurrentDates / SetDates)
    // --------------------------------------------------------------------------------------------------------------

    onPayrollAreaChange(value: string): void {
        this.payrollAreaName.set(value || '');
        this.refreshPeriod();
    }

    onPeriodModeChange(other: boolean): void {
        this.otherPeriod.set(other);
        this.refreshPeriod();
    }

    onMonthChange(value: any): void {
        this.month.set(value ? +value : undefined);
        if (this.otherPeriod()) {
            this.loadOtherPeriod();
        }
    }

    onYearChange(value: any): void {
        this.year.set(value ? +value : undefined);
        if (this.otherPeriod()) {
            this.loadOtherPeriod();
        }
    }

    private refreshPeriod(): void {
        if (this.otherPeriod()) {
            this.loadOtherPeriod();
        } else {
            this.loadCurrentPeriod();
        }
    }

    /** Legacy SetCurrentDates: month, year, start and end from the area's control record. */
    private loadCurrentPeriod(): void {
        const area = this.payrollAreaName();
        if (!area) {
            this.period.set(undefined);
            return;
        }

        this.periodLoading.set(true);
        this._projectCostEngineService
            .getProjectCostEngineContext(area)
            .pipe(finalize(() => this.periodLoading.set(false)))
            .subscribe((period) => {
                // The user may have changed area or mode while this loaded.
                if (this.payrollAreaName() !== area || this.otherPeriod()) {
                    return;
                }

                this.period.set(period);
                if (period.hasControlRecord) {
                    this.month.set(period.payrollMonth);
                    this.year.set(period.payrollYear);
                }
            });
    }

    /** Legacy SetDates: the typed month / year through the area's payroll year. */
    private loadOtherPeriod(): void {
        const area = this.payrollAreaName();
        const month = this.month();
        const year = this.year();

        if (!area || !month || !year || month < 1 || month > 12 || year < 1900 || year > 9999) {
            this.period.set(undefined);
            return;
        }

        this.periodLoading.set(true);
        this._projectCostEngineService
            .getProjectCostEnginePeriodDates(area, month, year)
            .pipe(finalize(() => this.periodLoading.set(false)))
            .subscribe((period) => {
                if (
                    this.payrollAreaName() !== area ||
                    !this.otherPeriod() ||
                    this.month() !== month ||
                    this.year() !== year
                ) {
                    return;
                }

                this.period.set(period);
            });
    }

    /** Legacy txtEndDate: the date the run is sent with. */
    payInDate(): DateTime | undefined {
        return this.period()?.calendarMonthEnd ?? undefined;
    }

    /** Shown under the period when the engine would refuse it (legacy only said so after the run). */
    periodNotRunnable(): boolean {
        const period = this.period();
        return !!period && !!period.calendarMonthEnd && period.hasControlRecord && !period.runAllowed;
    }

    // --------------------------------------------------------------------------------------------------------------
    // Employee picker (legacy popupEmployees)
    // --------------------------------------------------------------------------------------------------------------

    canPickEmployees(): boolean {
        // Legacy guard checks (BuildEmployeePopupList lines 1731-1733): area and end date.
        return !!this.payrollAreaName() && !!this.payInDate();
    }

    openEmployeePicker(): void {
        if (!this.canPickEmployees()) {
            return;
        }

        this.employeePicker().show({
            payrollAreaName: this.payrollAreaName(),
            payrollDate: this.payInDate(),
            selectedCodes: this.employeeCodes(),
        });
    }

    onEmployeesPicked(codes: string): void {
        this.employeeCodes.set(codes || '');
    }

    // --------------------------------------------------------------------------------------------------------------
    // Run / Stop / Clear Status (legacy ASPxCallbackPanel1_Callback)
    // --------------------------------------------------------------------------------------------------------------

    /** Legacy btnCalculateProjectCosting. */
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

        const codes = this.splitCodes(this.employeeCodes());
        if (codes.length) {
            this.confirmRun(this.l('PayrollRunEmployeesCount', codes.length));
            return;
        }

        // No codes = the area's employees; say how many before asking.
        this.running.set(true);
        this._projectCostEngineService
            .getProjectCostEngineCandidates(
                new GetProjectCostEngineCandidatesInput({
                    payrollAreaName: this.payrollAreaName(),
                    payrollDate: this.payInDate(),
                    statusFilter: PayrollRunCandidateFilter.All,
                    filter: undefined,
                    selectedEmployeeCodes: undefined,
                    sorting: undefined,
                    skipCount: 0,
                    maxResultCount: 1,
                })
            )
            .pipe(finalize(() => this.running.set(false)))
            .subscribe((result) => this.confirmRun(this.l('PayrollRunAllEmployeesCount', result.allCount)));
    }

    /** Legacy ran on the click; this asks first, with area, period and how many employees. */
    private confirmRun(employeesText: string): void {
        const text = [this.payrollAreaName(), this.formatDate(this.payInDate()), employeesText].join(' - ');

        this.message.confirm(text, this.l('RunProjectCostEngine') + '?', (isConfirmed) => {
            if (isConfirmed) {
                this.queueRun();
            }
        });
    }

    private queueRun(): void {
        const input = new RunProjectCostEngineInput({
            // Legacy's run type box was hidden and always Regular.
            payrollRunType: PayrollRunType.Regular,
            employeeCodes: this.employeeCodes() ? this.employeeCodes() : undefined,
            payInDate: this.payInDate(),
            payrollAreaName: this.payrollAreaName(),
            offCyclePaymentDate: undefined,
            offCycleReason: undefined,
        });

        const progress = this.engineProgress();
        const baselineRunId = progress?.status()?.hasRun ? progress.status().runId : undefined;

        this.running.set(true);
        this._projectCostEngineService
            .runProjectCostEngine(input)
            .pipe(finalize(() => this.running.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('ProjectCostEngineRunQueued'));
                this.engineProgress()?.onRunQueued();
                this._baselineRunId = baselineRunId;
                this.startLogPolling(true);
                this.reloadLogs();
            });
    }

    /** Legacy btnStopProjectCosting -- on the run itself. */
    stopProjectCostEngine(): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._projectCostEngineService.stopProjectCostEngine().subscribe(() => {
                this.notify.info(this.l('EngineRunStopRequested'));
                this.engineProgress()?.refreshStatus();
            });
        });
    }

    /** Legacy btnClearStatus: the log and the progress history (a run in progress is kept). */
    clearStatus(): void {
        this.message.confirm('', this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._projectCostEngineService.clearProjectCostEngineProgress().subscribe(() => {
                this.notify.info(this.l('ProjectCostEngineStatusCleared'));
                this.engineProgress()?.refreshStatus();
                this.engineProgress()?.reloadEmployees();
                this.reloadLogs();
            });
        });
    }

    refresh(): void {
        this.engineProgress()?.refreshStatus();
        this.engineProgress()?.reloadEmployees();
        this.reloadLogs();
    }

    // --------------------------------------------------------------------------------------------------------------
    // Run log (legacy log popup grid + btnExportTimeLog_Click)
    // --------------------------------------------------------------------------------------------------------------

    getLogs(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._projectCostEngineService
            .getProjectCostEngineLogs(
                new GetProjectCostEngineLogsInput({
                    onlyFailed: this.logOnlyFailed(),
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.logAllCount.set(result.allCount);
                this.logSuccessCount.set(result.successCount);
                this.logFailedCount.set(result.failedCount);
            });
    }

    reloadLogs(): void {
        const rows = this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getLogs({ first: (this.paginator()?.getPage() ?? 0) * rows, rows } as LazyLoadEvent);
    }

    setLogFilter(onlyFailed: boolean | undefined): void {
        this.logOnlyFailed.set(onlyFailed);
        // A filter change restarts at page 1, otherwise the operator lands on an empty page.
        this.getLogs({ first: 0, rows: this.primengTableHelper.defaultRecordsCountPerPage } as LazyLoadEvent);
    }

    /** Legacy btnExportTimeLog_Click: the log as filtered, every page. */
    exportLogsToExcel(): void {
        this.exporting.set(true);
        this._projectCostEngineService
            .getProjectCostEngineLogsToExcel(
                new GetProjectCostEngineLogsToExcelInput({ onlyFailed: this.logOnlyFailed() })
            )
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    logStatusText(status: number): string {
        return status === 0 ? this.l('Successfull') : this.l('Failed');
    }

    /**
     * Keeps the log fresh while a run is going; the shared panel polls its own status. After a Run the panel may still
     * show the PREVIOUS run until the job starts, so polling waits for a different run before it trusts "not running".
     */
    private startLogPolling(waitForNewRun: boolean): void {
        this._waitingForNewRun = waitForNewRun;
        this._waitTicks = 0;
        if (this._pollHandle) {
            return;
        }

        this._pollHandle = setInterval(() => {
            this.reloadLogs();

            const status = this.engineProgress()?.status();
            if (!status) {
                return;
            }

            if (this._waitingForNewRun) {
                // Give up waiting after five minutes: a job that has not started by then is not coming.
                this._waitTicks = this._waitTicks + 1;
                if ((!status.hasRun || status.runId === this._baselineRunId) && this._waitTicks < 60) {
                    return;
                }
                this._waitingForNewRun = false;
            }

            if (!status.isRunning) {
                this.stopLogPolling();
            }
        }, 5000);
    }

    private stopLogPolling(): void {
        if (this._pollHandle) {
            clearInterval(this._pollHandle);
            this._pollHandle = undefined;
        }
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
}
