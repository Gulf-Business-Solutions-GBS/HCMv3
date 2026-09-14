import { Component, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { LuxonFromNowPipe } from '@shared/utils/luxon-from-now.pipe';
import {
    BackgroundJobsOverviewDto,
    BackgroundJobsServiceProxy,
    QueuedJobState,
} from '@shared/service-proxies/service-proxies';

/**
 * Background Jobs: the scheduled tasks and how their latest run went, the queued jobs still waiting or stuck, and the
 * recent failures of queued jobs.
 *
 * READ-ONLY. Nothing here starts, stops or retries a job; it answers "did it run, and did it work?".
 *
 * WHO SEES WHAT is decided on the server, not here. The host sees every company; a company sees its own jobs and
 * failures, and a system-wide task's error arrives empty. This component only explains the empty column.
 *
 * ALL STATE IS SIGNALS -- the app runs zoneless, and a plain field set from an HTTP callback schedules no repaint.
 */
@Component({
    templateUrl: './background-jobs.component.html',
    styleUrl: './background-jobs.component.css',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, LocalizePipe, LuxonFormatPipe, LuxonFromNowPipe],
})
export class BackgroundJobsComponent extends AppComponentBase {
    private _backgroundJobsService = inject(BackgroundJobsServiceProxy);

    readonly QueuedJobState = QueuedJobState;

    readonly isLoading = signal<boolean>(false);
    readonly overview = signal<BackgroundJobsOverviewDto | null>(null);

    readonly isHost = computed(() => this.overview()?.isHost ?? false);
    readonly tasks = computed(() => this.overview()?.scheduledTasks ?? []);
    readonly queuedJobs = computed(() => this.overview()?.queuedJobs ?? []);
    readonly failures = computed(() => this.overview()?.recentFailures ?? []);

    /** A task that failed on its latest run, so the header can say so without the reader scanning the table. */
    readonly failingTaskCount = computed(() => this.tasks().filter((t) => t.lastSucceeded === false).length);

    constructor() {
        super();
        this.load();
    }

    load(): void {
        this.isLoading.set(true);
        this._backgroundJobsService
            .getOverview()
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => this.overview.set(result));
    }

    /** "5 min", "1 h", "1 d" -- the task intervals are always whole minutes, hours or days. */
    interval(minutes: number | undefined): string {
        if (minutes == null) {
            return '';
        }

        if (minutes >= 1440 && minutes % 1440 === 0) {
            return this.l('BgDays', minutes / 1440);
        }

        if (minutes >= 60 && minutes % 60 === 0) {
            return this.l('BgHours', minutes / 60);
        }

        return this.l('BgMinutes', minutes);
    }

    stateLabel(state: QueuedJobState): string {
        switch (state) {
            case QueuedJobState.Retrying:
                return this.l('BgStateRetrying');
            case QueuedJobState.Abandoned:
                return this.l('BgStateAbandoned');
            default:
                return this.l('BgStateWaiting');
        }
    }

    stateClass(state: QueuedJobState): string {
        switch (state) {
            case QueuedJobState.Retrying:
                return 'badge-light-warning';
            case QueuedJobState.Abandoned:
                return 'badge-light-danger';
            default:
                return 'badge-light-primary';
        }
    }
}
