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
    EngineRunEmployeeDto,
    EngineType,
    ExecutionLogDto,
    ExecutionLogStatus,
    GetAllPayrollAreasInput,
    GetTimeEngineRunHistoryInput,
    GetTimeEngineRunLogToExcelInput,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    RunTimeEngineInput,
    TimeEngineServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { EngineRunProgressComponent } from '@app/shared/common/engine-run-progress/engine-run-progress.component';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { TimeEngineEmployeePickerModalComponent } from './time-engine-employee-picker-modal.component';
import { TimeEngineRunDaysModalComponent } from './time-engine-run-days-modal.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Port of the legacy Time Engine run screen
 * (HCM10.Web/UserControls/PayrollManagement/uscTimeEngine.ascx + .ascx.cs).
 *
 * Legacy shelled out to HCM10.TimeEngine/Program.Main on a ThreadPool work item and polled a
 * DevExpress timer for progress. Here the run is enqueued as an ABP background job by
 * ITimeEngineAppService.RunTimeEngine and saved ten employees at a time; the live panel follows the run of the
 * payroll area selected here (one run per area at a time), each employee row opens its days (legacy's per-day
 * TimeLog grid), the log exports to Excel (legacy btnExportTimeLog_Click), and the "..." button opens legacy's
 * employee popup (Processed / Pending). The run history is the Time Engine's execution log, without the
 * per-employee runs the payroll engine makes for itself.
 */
@Component({
    templateUrl: './time-engine.component.html',
    animations: [appModuleAnimation()],
    imports: [
        HelpPageDirective,
        DateInputDirective,
        SubHeaderComponent,
        EngineRunProgressComponent,
        TimeEngineEmployeePickerModalComponent,
        TimeEngineRunDaysModalComponent,
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
    private _payrollAreasService = inject(PayrollAreasServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);

    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    /** The shared live-progress panel; told to start polling as soon as a run is queued. */
    readonly engineProgress = viewChild<EngineRunProgressComponent>('engineProgress');
    readonly employeePicker = viewChild<TimeEngineEmployeePickerModalComponent>('employeePicker');
    readonly runDaysModal = viewChild<TimeEngineRunDaysModalComponent>('runDaysModal');

    readonly EngineType = EngineType;

    // Legacy args[0..3] -- see uscTimeEngine.ascx.cs ExecuteLongOperation().
    readonly employeeCodes = signal<string>('');
    readonly startDate = signal<string>('');
    readonly endDate = signal<string>('');
    readonly payrollAreaName = signal<string>('');

    readonly payrollAreas = signal<PayrollAreaDto[]>([]);
    readonly running = signal<boolean>(false);
    readonly exporting = signal<boolean>(false);

    private _pollHandle: any;

    ngOnInit(): void {
        // The PrimeNG table's (onLazyLoad) fires the first getRunHistory() -- do not call it here.
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

    // --------------------------------------------------------------------------------------------------------------
    // Payroll area: the live panel follows the selected area's run
    // --------------------------------------------------------------------------------------------------------------

    onPayrollAreaChanged(value: string): void {
        this.payrollAreaName.set(value || '');

        // The panel reads the area through its input, which takes the new value on the next change detection.
        setTimeout(() => this.engineProgress()?.reload());
    }

    // --------------------------------------------------------------------------------------------------------------
    // Employee picker (legacy btnSelectEmployees / popupTimeEmployees)
    // --------------------------------------------------------------------------------------------------------------

    /** Legacy BuildTimeEmployeePopupList lists nobody without a payroll area. */
    canPickEmployees(): boolean {
        return !!this.payrollAreaName();
    }

    openEmployeePicker(): void {
        if (!this.canPickEmployees()) {
            return;
        }

        this.employeePicker().show({
            payrollAreaName: this.payrollAreaName(),
            startDate: this.startDate(),
            endDate: this.endDate(),
            selectedCodes: this.employeeCodes(),
        });
    }

    onEmployeesPicked(codes: string): void {
        this.employeeCodes.set(codes || '');
    }

    // --------------------------------------------------------------------------------------------------------------
    // Run
    // --------------------------------------------------------------------------------------------------------------

    runTimeEngine(): void {
        // Same order the app service validates in (TimeEngineAppService.RunTimeEngine), so the client surfaces the
        // same message the server would have.
        if (!this.endDate()) {
            this.message.warn(this.l('TimeEngineEndDateRequired'));
            return;
        }

        if (this.startDate() && this.startDate() > this.endDate()) {
            this.message.warn(this.l('TimeEngineStartDateAfterEndDate'));
            return;
        }

        // Legacy: "Cannot have PayrollArea unselected if the EmpCode is blank."
        if (!this.payrollAreaName() && !(this.employeeCodes() || '').trim()) {
            this.message.warn(this.l('TimeEnginePayrollAreaOrEmployeeCodeRequired'));
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

    // --------------------------------------------------------------------------------------------------------------
    // Day-level log and export (legacy popupPayrollLog / btnExportTimeLog_Click)
    // --------------------------------------------------------------------------------------------------------------

    showEmployeeDays(employee: EngineRunEmployeeDto): void {
        const runId = this.engineProgress()?.status()?.runId;
        if (!runId) {
            return;
        }

        this.runDaysModal().show(runId, employee);
    }

    /** The run the panel shows, with the panel's filters: one row per employee per day, as legacy exported. */
    exportRunLogToExcel(): void {
        const progress = this.engineProgress();

        this.exporting.set(true);
        this._timeEngineService
            .getTimeEngineRunLogToExcel(
                new GetTimeEngineRunLogToExcelInput({
                    runId: progress?.status()?.hasRun ? progress.status().runId : undefined,
                    payrollAreaName: this.payrollAreaName() ? this.payrollAreaName() : undefined,
                    filter: progress?.filterText() ? progress.filterText() : undefined,
                    onlyFailed: progress?.onlyFailed(),
                })
            )
            .pipe(finalize(() => this.exporting.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    // --------------------------------------------------------------------------------------------------------------
    // Run history
    // --------------------------------------------------------------------------------------------------------------

    getRunHistory(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);
            return;
        }

        this.primengTableHelper.showLoadingIndicator();

        this._timeEngineService
            .getTimeEngineRunHistory(
                new GetTimeEngineRunHistoryInput({
                    sorting: this.primengTableHelper.getSorting(this.dataTable()),
                    maxResultCount: this.primengTableHelper.getMaxResultCount(this.paginator(), event),
                    skipCount: this.primengTableHelper.getSkipCount(this.paginator(), event),
                })
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);

                const inProgress = (result.items || []).some(
                    (item: ExecutionLogDto) => item.executionStatus === ExecutionLogStatus.INPRECESS
                );
                if (!inProgress) {
                    this.stopPolling();
                }

                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator()?.rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getRunHistory({ first: (this.paginator()?.getPage() ?? 0) * rows, rows } as LazyLoadEvent);
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
