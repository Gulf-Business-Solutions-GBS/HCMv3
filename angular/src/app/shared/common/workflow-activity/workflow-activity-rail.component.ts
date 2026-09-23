import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { EmployeeAvatarComponent } from '@shared/common/employee-avatar/employee-avatar.component';
import {
    AddWorkflowCommentInput,
    EditWorkflowCommentInput,
    EntityDto,
    GetWorkflowActivityFeedInput,
    RequestActionType,
    WorkflowActivityDto,
    WorkflowActivityFeedDto,
    WorkflowActivityKind,
    WorkflowActivityServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';

/**
 * The activity rail: one request's action trail and discussion, in a single chronological stream, with a
 * box to add a comment.
 *
 * <p>Replaces two separate legacy surfaces -- a Syncfusion grid of RequestLog rows, and a read-only
 * "Workflow Comments" textarea that approvers' comments were string-concatenated onto with their name and a
 * timestamp. Because the server now returns an employee id per entry rather than a name string, entries can
 * carry the author's photo, which the old blob could never do.</p>
 *
 * <p>Reused unchanged in three places: the approval screen, the requester's view of their own request, and
 * the Workflow History button on the admin transaction screen. Pass readOnly for the last of those.</p>
 */
@Component({
    selector: 'workflow-activity-rail',
    templateUrl: './workflow-activity-rail.component.html',
    styleUrls: ['./workflow-activity-rail.component.less'],
    imports: [CommonModule, FormsModule, LocalizePipe, LuxonFormatPipe, EmployeeAvatarComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowActivityRailComponent extends AppComponentBase implements OnInit {
    private readonly _activityService = inject(WorkflowActivityServiceProxy);

    readonly workflowRequestId = input.required<number>();

    /** History view: the feed is shown but no comment can be added. */
    readonly readOnly = input(false);

    readonly loading = signal(false);
    readonly saving = signal(false);
    readonly feed = signal<WorkflowActivityFeedDto | undefined>(undefined);

    /** Bound to the composer. Internal defaults to false so a comment is never private by accident. */
    newComment = '';
    newCommentIsInternal = false;

    editingId = signal<number | undefined>(undefined);
    editingBody = '';

    readonly activities = computed(() => this.feed()?.activities ?? []);
    readonly canComment = computed(() => !this.readOnly() && (this.feed()?.canComment ?? false));
    readonly canCommentInternally = computed(() => this.feed()?.canCommentInternally ?? false);

    readonly Kind = WorkflowActivityKind;

    constructor() {
        super();

        // Reload when the host switches which request is shown.
        effect(() => {
            const id = this.workflowRequestId();
            if (id) {
                this.load(id);
            }
        });
    }

    ngOnInit(): void {
        // The effect above covers the initial load too; this keeps OnInit explicit for readers.
    }

    load(requestId: number): void {
        this.loading.set(true);

        const input = new GetWorkflowActivityFeedInput();
        input.workflowRequestId = requestId;

        this._activityService
            .getActivityFeed(input)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.feed.set(result));
    }

    addComment(): void {
        const body = (this.newComment || '').trim();
        if (!body || this.saving()) {
            return;
        }

        const input = new AddWorkflowCommentInput();
        input.workflowRequestId = this.workflowRequestId();
        input.body = body;
        input.isInternal = this.newCommentIsInternal;

        this.saving.set(true);

        this._activityService
            .addComment(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.newComment = '';
                this.newCommentIsInternal = false;

                // Reloaded rather than appended locally: the server decides ordering and what this viewer is
                // allowed to see, and duplicating that here is how the two drift apart.
                this.load(this.workflowRequestId());
            });
    }

    startEdit(activity: WorkflowActivityDto): void {
        this.editingId.set(activity.id);
        this.editingBody = activity.body;
    }

    cancelEdit(): void {
        this.editingId.set(undefined);
        this.editingBody = '';
    }

    saveEdit(): void {
        const body = (this.editingBody || '').trim();
        const id = this.editingId();
        if (!body || !id) {
            return;
        }

        const input = new EditWorkflowCommentInput();
        input.id = id;
        input.body = body;

        this.saving.set(true);

        this._activityService
            .editComment(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.cancelEdit();
                this.load(this.workflowRequestId());
            });
    }

    deleteComment(activity: WorkflowActivityDto): void {
        this.message.confirm(this.l('CommentDeleteWarningMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._activityService
                .deleteComment(activity.id)
                .subscribe(() => this.load(this.workflowRequestId()));
        });
    }

    actorName(activity: WorkflowActivityDto): string {
        return this.isRtl ? activity.actorNameAr || activity.actorNameEn : activity.actorNameEn;
    }

    /** Two letters, so an entry still reads as a person when no photo exists. */
    initials(activity: WorkflowActivityDto): string {
        const name = (this.actorName(activity) || '').trim();
        if (!name) {
            return '?';
        }

        const parts = name.split(/\s+/);
        return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
    }

    /**
     * Action entries read as a sentence -- "Approved by Ahlam Ahmad Seblani" -- rather than as a row of
     * columns, which is how the legacy grid presented the same data.
     */
    actionLabel(activity: WorkflowActivityDto): string {
        // Keyed on the enum NAME, not its number, so the localization keys stay readable
        // (WorkflowAction_Approved) and survive anyone reordering the enum.
        const name = RequestActionType[activity.actionType];

        return name ? this.l('WorkflowAction_' + name) : '';
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
