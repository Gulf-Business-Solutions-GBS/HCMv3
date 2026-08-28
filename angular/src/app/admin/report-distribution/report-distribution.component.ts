import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import {
    ReportSchedulesServiceProxy,
    ReportScheduleListDto,
    GetAllReportSchedulesInput,
    SetReportScheduleActiveInput,
    ReportRunStatus,
    ReportBurstDimension,
    EntityDto,
} from '@shared/service-proxies/service-proxies';

/**
 * Report Distribution: the list of report schedules, with the row actions from the customer's
 * reference screen (Run, Edit, Delete, Duplicate, New Schedule).
 *
 * The list deliberately surfaces LAST RUN STATUS rather than only "active/inactive". Report
 * distribution fails silently -- nobody notices an e-mail that was never sent -- so a schedule that
 * has been failing for three months must be visible here without opening anything.
 */
@Component({
    templateUrl: './report-distribution.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, TableModule, Tooltip, LocalizePipe, PermissionPipe, DatePipe],
})
export class ReportDistributionComponent extends AppComponentBase {
    private _service = inject(ReportSchedulesServiceProxy);
    private _router = inject(Router);

    readonly isLoading = signal<boolean>(false);
    readonly records = signal<ReportScheduleListDto[]>([]);
    readonly totalCount = signal<number>(0);

    filterText = '';

    /** Expose the enum to the template so status chips can be compared without magic numbers. */
    readonly RunStatus = ReportRunStatus;

    // Deliberately no getRecords() in ngOnInit: the PrimeNG table's (onLazyLoad) fires on init and
    // calling both produces a duplicated first request.
    getRecords(event?: TableLazyLoadEvent): void {
        const input = new GetAllReportSchedulesInput();
        input.filter = this.filterText;
        input.skipCount = event?.first ?? 0;
        input.maxResultCount = event?.rows ?? 10;
        input.sorting = this.primengTableHelper?.getSorting(event as any) ?? undefined;

        this.isLoading.set(true);
        this._service
            .getAll(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => {
                this.records.set(result.items ?? []);
                this.totalCount.set(result.totalCount);
            });
    }

    createSchedule(): void {
        this._router.navigate(['/app/admin/report-distribution/wizard']);
    }

    editSchedule(record: ReportScheduleListDto): void {
        this._router.navigate(['/app/admin/report-distribution/wizard', record.id]);
    }

    viewRuns(record: ReportScheduleListDto): void {
        this._router.navigate(['/app/admin/report-distribution/runs', record.id]);
    }

    /**
     * Triggers a schedule immediately. The result is reported with the SLICE and RECIPIENT counts
     * rather than a bare "done", because the most common misconfiguration -- recipient rules that
     * match nobody -- otherwise looks exactly like a successful run.
     */
    runNow(record: ReportScheduleListDto): void {
        this.message.confirm(
            this.l('RunScheduleNowConfirmation', record.name),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (!isConfirmed) {
                    return;
                }

                this.isLoading.set(true);
                this._service
                    .runNow(new EntityDto({ id: record.id }))
                    .pipe(finalize(() => this.isLoading.set(false)))
                    .subscribe((result) => {
                        if (result.sliceCount === 0) {
                            this.notify.warn(result.message ?? this.l('ScheduleResolvedNoRecipients'));
                        } else {
                            this.notify.success(
                                this.l('ScheduleRunQueued', result.sliceCount, result.recipientCount)
                            );
                        }

                        this.getRecords();
                    });
            }
        );
    }

    toggleActive(record: ReportScheduleListDto): void {
        const input = new SetReportScheduleActiveInput();
        input.id = record.id;
        input.isActive = !record.isActive;

        this._service.setActive(input).subscribe(() => {
            this.notify.success(this.l('SavedSuccessfully'));
            this.getRecords();
        });
    }

    duplicateSchedule(record: ReportScheduleListDto): void {
        this._service.duplicate(new EntityDto({ id: record.id })).subscribe((newId) => {
            // The copy is created INACTIVE by the server so it cannot start mailing people before
            // anybody has adjusted it. Say so, otherwise the user assumes it is already live.
            this.notify.info(this.l('ScheduleDuplicatedAsInactive'));
            this._router.navigate(['/app/admin/report-distribution/wizard', newId]);
        });
    }

    deleteSchedule(record: ReportScheduleListDto): void {
        this.message.confirm(this.l('ScheduleDeleteWarning', record.name), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._service.delete(record.id).subscribe(() => {
                this.notify.success(this.l('SuccessfullyDeleted'));
                this.getRecords();
            });
        });
    }

    /** Enum name for the burst column; the raw numeric value means nothing to a user. */
    burstText(dimension: ReportBurstDimension | undefined): string {
        return dimension === undefined || dimension === null ? '' : ReportBurstDimension[dimension];
    }

    statusClass(status: ReportRunStatus | undefined): string {
        switch (status) {
            case ReportRunStatus.Succeeded:
                return 'badge badge-success';
            case ReportRunStatus.PartiallySucceeded:
                return 'badge badge-warning';
            case ReportRunStatus.Failed:
                return 'badge badge-danger';
            case ReportRunStatus.SkippedEmpty:
                return 'badge badge-secondary';
            default:
                return 'badge badge-light';
        }
    }

    statusText(status: ReportRunStatus | undefined): string {
        if (status === undefined || status === null) {
            return this.l('NeverRun');
        }

        return ReportRunStatus[status];
    }
}
