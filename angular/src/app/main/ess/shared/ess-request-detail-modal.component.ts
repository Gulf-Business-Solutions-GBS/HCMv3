import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { EmployeeAvatarComponent } from '@shared/common/employee-avatar/employee-avatar.component';
import {
    GetWorkflowActivityFeedInput,
    RequestActionType,
    RequestStatus,
    WorkflowActivityDto,
    WorkflowActivityFeedDto,
    WorkflowActivityKind,
    WorkflowActivityServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';

/** One read-only field on the Request Detail card. */
export interface EssRequestDetailField {
    label: string;
    value: string;
}

/**
 * Everything the modal needs about a request. Passed in rather than fetched, because the list row the
 * employee clicked already carries it -- and because that keeps this component usable by all 21
 * self-service screens without one of them needing a bespoke "get for view" endpoint.
 */
export interface EssRequestDetailConfig {
    title: string;
    fields: EssRequestDetailField[];
    comments?: string;
    workflowRequestId?: number;
    statusLabel: string;
    statusClass: string;
}

/**
 * A submitted self-service request, opened read-only.
 *
 * <p>Fills the gap the user found: the list showed a status badge and a withdraw link, so an employee could
 * see THAT their request was waiting but never WHAT they had submitted. Mirrors the legacy request form's
 * four cards -- Request Detail, Request Comment, Workflow Comment (readonly), Workflow Actions &
 * Information -- with the last one a tabbed panel over the activity feed.</p>
 *
 * <p>Read-only on purpose. Once a request is in the chain the workflow owns the row; the only thing the
 * requester can still do is withdraw it, and that stays on the list where it was.</p>
 */
@Component({
    selector: 'essRequestDetailModal',
    templateUrl: './ess-request-detail-modal.component.html',
    imports: [CommonModule, AppBsModalDirective, LocalizePipe, LuxonFormatPipe, EmployeeAvatarComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EssRequestDetailModalComponent extends AppComponentBase {
    private readonly _activityService = inject(WorkflowActivityServiceProxy);

    readonly modal = viewChild<ModalDirective>('essRequestDetailModal');

    readonly active = signal(false);
    readonly loading = signal(false);
    readonly config = signal<EssRequestDetailConfig | undefined>(undefined);
    readonly feed = signal<WorkflowActivityFeedDto | undefined>(undefined);

    /** 'request' | 'log' | 'readLog' -- the legacy panel's tabs, minus the two with no data behind them. */
    readonly activeTab = signal<'request' | 'log' | 'readLog'>('request');

    private readonly activities = computed(() => this.feed()?.activities ?? []);

    /**
     * What approvers wrote, as its own card. Legacy keeps these in a read-only "Workflow Comment" box
     * separate from the requester's own comment, and the distinction is worth keeping: one is what you
     * said, the other is what was said back.
     */
    readonly workflowComments = computed(() =>
        this.activities().filter((a) => a.kind === WorkflowActivityKind.Comment && !a.isInternal)
    );

    /** The audit trail, minus reads -- those are their own tab, as in legacy. */
    readonly requestLog = computed(() =>
        this.activities().filter((a) => a.kind === WorkflowActivityKind.Action && a.actionType !== RequestActionType.Read)
    );

    readonly readLog = computed(() =>
        this.activities().filter((a) => a.kind === WorkflowActivityKind.Action && a.actionType === RequestActionType.Read)
    );

    /** The step the request has reached: the latest one any action was logged against. */
    readonly currentStep = computed(() => {
        const steps = this.activities()
            .map((a) => a.workflowStep)
            .filter((s): s is number => s !== undefined && s !== null);

        return steps.length ? Math.max(...steps) : undefined;
    });

    show(config: EssRequestDetailConfig): void {
        this.config.set(config);
        this.feed.set(undefined);
        this.activeTab.set('request');
        this.active.set(true);
        this.modal()?.show();

        // A request that never entered a chain (recorded directly by HR) has no workflow side to show.
        if (config.workflowRequestId) {
            this.loadFeed(config.workflowRequestId);
        }
    }

    close(): void {
        this.active.set(false);
        this.config.set(undefined);
        this.feed.set(undefined);
        this.modal()?.hide();
    }

    private loadFeed(workflowRequestId: number): void {
        this.loading.set(true);

        const input = new GetWorkflowActivityFeedInput();
        input.workflowRequestId = workflowRequestId;

        this._activityService
            .getActivityFeed(input)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.feed.set(result));
    }

    actorName(activity: WorkflowActivityDto): string {
        return (this.isRtl ? activity.actorNameAr || activity.actorNameEn : activity.actorNameEn) ?? '';
    }

    /** Keyed on the enum NAME so the keys stay readable and survive anyone reordering the enum. */
    actionLabel(activity: WorkflowActivityDto): string {
        const name = RequestActionType[activity.actionType];

        return name ? this.l('WorkflowAction_' + name) : '';
    }

    requestStatusLabel(): string {
        const status = this.feed()?.requestStatus;
        if (status === undefined || status === null) {
            return this.config()?.statusLabel ?? '';
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[status]);
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
