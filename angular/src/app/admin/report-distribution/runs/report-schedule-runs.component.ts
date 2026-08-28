import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    ReportSchedulesServiceProxy,
    ReportScheduleRunDto,
    ReportScheduleRunSliceDto,
    GetReportScheduleRunsInput,
    ReportRunStatus,
    ReportDestinationMethod,
} from '@shared/service-proxies/service-proxies';

/**
 * Run history for one report schedule, with a per-recipient drill-down.
 *
 * WHY THIS SCREEN EARNS ITS PLACE. Report distribution fails silently by nature -- nobody notices an
 * e-mail that was never sent, and by the time a recipient complains the period is gone. The run log
 * already records every slice, but until now there was no way to read it, so the module could only be
 * trusted rather than checked.
 *
 * The drill-down answers the three questions people actually ask about a delivery, in this order:
 * did it go, who got it, and was it scoped to what that person may see.
 */
@Component({
    templateUrl: './report-schedule-runs.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe, DatePipe],
})
export class ReportScheduleRunsComponent extends AppComponentBase implements OnInit {
    private _service = inject(ReportSchedulesServiceProxy);
    private _route = inject(ActivatedRoute);
    private _router = inject(Router);

    readonly isLoading = signal<boolean>(false);
    readonly isLoadingSlices = signal<boolean>(false);

    readonly runs = signal<ReportScheduleRunDto[]>([]);
    readonly slices = signal<ReportScheduleRunSliceDto[]>([]);
    readonly selectedRunId = signal<number | null>(null);

    private scheduleId: number;

    ngOnInit(): void {
        this.scheduleId = +this._route.snapshot.params['id'];
        this.loadRuns();
    }

    loadRuns(): void {
        const input = new GetReportScheduleRunsInput();
        input.reportScheduleId = this.scheduleId;
        input.maxResultCount = 50;
        input.skipCount = 0;

        this.isLoading.set(true);
        this._service
            .getRuns(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => {
                this.runs.set(result.items ?? []);

                // Open the most recent run straight away: somebody arriving here almost always wants
                // "what happened last time", not a list to click through first.
                const mostRecent = this.runs()[0];
                if (mostRecent) {
                    this.selectRun(mostRecent);
                }
            });
    }

    selectRun(run: ReportScheduleRunDto): void {
        this.selectedRunId.set(run.id);

        this.isLoadingSlices.set(true);
        this._service
            .getRunSlices(run.id)
            .pipe(finalize(() => this.isLoadingSlices.set(false)))
            .subscribe((result) => this.slices.set(result.items ?? []));
    }

    back(): void {
        this._router.navigate(['/app/admin/report-distribution']);
    }

    /**
     * The proxies type timestamps as luxon DateTime, which Angular's date pipe does not accept -- it
     * wants a Date, a number or a string. Converting here keeps the template free of casts that would
     * otherwise hide the mismatch rather than fix it.
     */
    toDate(value: any): Date | null {
        if (!value) {
            return null;
        }

        return typeof value.toJSDate === 'function' ? value.toJSDate() : new Date(value);
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
            case ReportRunStatus.Running:
                return 'badge badge-info';
            default:
                return 'badge badge-light';
        }
    }

    statusText(status: ReportRunStatus | undefined): string {
        return status === undefined || status === null ? '' : ReportRunStatus[status];
    }

    methodText(method: ReportDestinationMethod | undefined): string {
        return method === undefined || method === null ? '' : ReportDestinationMethod[method];
    }

    /**
     * Explains a SkippedEmpty slice.
     *
     * Without this the row reads as a failure to anybody scanning the log, when it is usually the
     * module working correctly: the recipient was entitled to nothing within that slice, so nothing
     * was sent rather than an empty attachment being mailed.
     */
    emptyExplanation(slice: ReportScheduleRunSliceDto): string {
        if (slice.status !== ReportRunStatus.SkippedEmpty) {
            return '';
        }

        return slice.wasRecipientScoped
            ? this.l('SkippedEmptyScopedHint')
            : this.l('SkippedEmptyHint');
    }
}
