import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeAvatarComponent } from '@shared/common/employee-avatar/employee-avatar.component';
import {
    AddWorkflowCommentInput,
    EditWorkflowCommentInput,
    GetWorkflowActivityFeedInput,
    RequestActionType,
    WorkflowActivityDto,
    WorkflowActivityFeedDto,
    WorkflowActivityKind,
    WorkflowActivityServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';

/**
 * One post in the conversation, after the two things that can produce one -- a server activity, and the
 * comment the requester typed on the request itself -- have been flattened into a single shape.
 *
 * <p>Flattened here rather than branched in the template because the requester's comment is not an activity
 * on the server (it is a column on the transaction row) yet has to read as the first post in the thread. A
 * template that special-cased it would need the whole entry layout twice.</p>
 */
export interface WorkflowFeedEntry {
    /** Stable across reloads, so @for does not tear down and refetch every avatar on each poll. */
    key: string;

    /** Zero for the requester's own comment, which has no activity row behind it. */
    id: number;

    /**
     * System events -- submitted, approved, read -- render as a light single line; human comments render as
     * a bubble. The whole visual split between "what happened" and "what was said" hangs off this.
     */
    isSystem: boolean;

    authorName: string;
    authorPhotoUrl: string | undefined;
    occurredAt: DateTime | undefined;
    body: string | undefined;

    /** Only set for system entries and feedback requests: "Approved", "Sent back". */
    actionLabel: string;

    isInternal: boolean;
    isEdited: boolean;
    canEdit: boolean;
}

/**
 * One request's conversation: what was said and what happened, in a single social-feed column with a box to
 * add a comment.
 *
 * <p>Replaces three separate legacy surfaces -- a Syncfusion grid of RequestLog rows, a read-only "Workflow
 * Comments" textarea that approvers' comments were string-concatenated onto with their name and a timestamp,
 * and a "Request Comment" box holding the requester's own note. The user asked for SuccessFactors' single
 * threaded stream and explicitly rejected the paired-textarea layout, so all three now render as posts in
 * one column. Because the server returns an employee id per entry rather than a name string, entries carry
 * the author's photo, which the concatenated blob never could.</p>
 *
 * <p>Reused in four places: the approval inbox, the ESS request detail modal, the requester's view of their
 * own request, and the Workflow History button on the admin transaction screen. Pass readOnly for history,
 * and flush when the host already supplies a card to sit in.</p>
 */
@Component({
    selector: 'workflow-activity-rail',
    templateUrl: './workflow-activity-rail.component.html',
    styleUrls: ['./workflow-activity-rail.component.less'],
    imports: [CommonModule, FormsModule, LocalizePipe, EmployeeAvatarComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowActivityRailComponent extends AppComponentBase implements OnInit {
    private readonly _activityService = inject(WorkflowActivityServiceProxy);

    readonly workflowRequestId = input.required<number>();

    /** History view: the feed is shown but no comment can be added. */
    readonly readOnly = input(false);

    /** Drops this component's own border and height cap, for hosts that already provide a card. */
    readonly flush = input(false);

    /** Hidden when the host's card title already says what this is. */
    readonly showHeader = input(true);

    /**
     * The comment the requester typed on the request itself, shown as the first post in the thread.
     *
     * <p>It stays a field on the transaction -- still edited on the create form -- and only its DISPLAY
     * moved here, because the user wanted one conversation rather than a box sitting above one.</p>
     */
    readonly leadComment = input<string | undefined>(undefined);
    readonly leadAuthorName = input<string>('');
    readonly leadAuthorPhotoUrl = input<string | undefined>(undefined);

    readonly loading = signal(false);
    readonly saving = signal(false);
    readonly feed = signal<WorkflowActivityFeedDto | undefined>(undefined);

    /**
     * Bound to the composer. There is no internal option any more -- see the note in the template. Every
     * comment written here is a comment on the request, readable by everyone the request involves.
     */
    newComment = '';

    editingId = signal<number | undefined>(undefined);
    editingBody = '';

    readonly activities = computed(() => this.feed()?.activities ?? []);
    readonly canComment = computed(() => !this.readOnly() && (this.feed()?.canComment ?? false));

    readonly Kind = WorkflowActivityKind;

    /**
     * The requester's own comment, attributed to whoever created the request.
     *
     * <p>The feed DTO carries no requester identity of its own, so the author falls back to the actor on the
     * Created activity, who is the requester by definition. Hosts that already know the name -- the ESS
     * screens do, it is the signed-in employee -- pass it in and skip the guess.</p>
     */
    private readonly leadEntry = computed<WorkflowFeedEntry | undefined>(() => {
        const body = (this.leadComment() ?? '').trim();
        if (!body) {
            return undefined;
        }

        const activities = this.activities();
        const created =
            activities.find((a) => a.kind === WorkflowActivityKind.Action && a.actionType === RequestActionType.Created) ??
            activities[0];

        // The engine now writes the submission comment onto the Created log entry, so for anything submitted
        // since that change the feed already opens with the requester's words. Adding the lead bubble too
        // would print them twice, one line apart.
        //
        // The lead is kept rather than deleted because requests submitted BEFORE that change have
        // remark: null on their Created row, and this is the only place their comment can come from. So the
        // rule is "show it unless the feed already has it", which is right for both.
        if (created?.body?.trim()) {
            return undefined;
        }

        return {
            key: 'lead',
            id: 0,
            isSystem: false,
            authorName: this.leadAuthorName() || (created ? this.actorName(created) : ''),
            authorPhotoUrl: this.leadAuthorPhotoUrl() ?? created?.actorPhotoUrl,
            occurredAt: created?.occurredAt,
            body,
            actionLabel: '',
            isInternal: false,
            isEdited: false,
            canEdit: false,
        };
    });

    readonly entries = computed<WorkflowFeedEntry[]>(() => {
        const mapped = this.activities().map((a) => this.toEntry(a));
        const lead = this.leadEntry();

        return lead ? [lead, ...mapped] : mapped;
    });

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

        // Always public. The composer no longer offers the choice, and defaulting the other way would make
        // a comment invisible to the very person it is usually addressed to.
        input.isInternal = false;

        this.saving.set(true);

        this._activityService
            .addComment(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.newComment = '';

                // Reloaded rather than appended locally: the server decides ordering and what this viewer is
                // allowed to see, and duplicating that here is how the two drift apart.
                this.load(this.workflowRequestId());
            });
    }

    startEdit(entry: WorkflowFeedEntry): void {
        this.editingId.set(entry.id);
        this.editingBody = entry.body ?? '';
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

    deleteComment(entry: WorkflowFeedEntry): void {
        this.message.confirm(this.l('CommentDeleteWarningMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._activityService.deleteComment(entry.id).subscribe(() => this.load(this.workflowRequestId()));
        });
    }

    actorName(activity: WorkflowActivityDto): string {
        return (this.isRtl ? activity.actorNameAr || activity.actorNameEn : activity.actorNameEn) ?? '';
    }

    /**
     * "2 hours ago" rather than a timestamp, which is most of what makes a column read as a conversation
     * instead of a log. The exact moment stays available: the template hangs it off the title attribute.
     */
    timeAgo(occurredAt: DateTime | undefined): string {
        return occurredAt?.toRelative() ?? '';
    }

    /**
     * The full moment, for the tooltip behind the relative one.
     *
     * <p>A method rather than the luxonFormat pipe because occurredAt is optional on an entry and the pipe
     * takes a non-nullable DateTime, which strict template checking rejects.</p>
     */
    timeExact(occurredAt: DateTime | undefined): string {
        return occurredAt?.toFormat('dd MMM yyyy HH:mm') ?? '';
    }

    /**
     * Action entries read as a sentence -- "Ahlam Ahmad Seblani approved this" -- rather than as a row of
     * columns, which is how the legacy grid presented the same data.
     */
    actionLabel(activity: WorkflowActivityDto): string {
        // Keyed on the enum NAME, not its number, so the localization keys stay readable
        // (WorkflowAction_Approved) and survive anyone reordering the enum.
        const name = RequestActionType[activity.actionType];

        return name ? this.l('WorkflowAction_' + name) : '';
    }

    private toEntry(activity: WorkflowActivityDto): WorkflowFeedEntry {
        const isSystem = activity.kind === WorkflowActivityKind.Action;

        return {
            key: activity.kind + '-' + activity.id,
            id: activity.id,
            isSystem,
            authorName: this.actorName(activity),
            authorPhotoUrl: activity.actorPhotoUrl,
            occurredAt: activity.occurredAt,
            body: activity.body,
            actionLabel: isSystem
                ? this.actionLabel(activity)
                : activity.kind === WorkflowActivityKind.FeedbackRequested
                  ? this.l('FeedbackRequestedFrom', this.actorName(activity))
                  : '',
            isInternal: activity.isInternal,
            isEdited: activity.isEdited,
            canEdit: activity.canEdit,
        };
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
