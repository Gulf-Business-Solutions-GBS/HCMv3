import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeAvatarComponent } from '@shared/common/employee-avatar/employee-avatar.component';
import {
    GetWorkflowParticipantsInput,
    RequestActionType,
    RequestStatus,
    WorkflowActivityServiceProxy,
    WorkflowApproverEntryDto,
    WorkflowApproverOrigin,
    WorkflowDelegateEntryDto,
    WorkflowDelegationRelation,
    WorkflowParticipantsDto,
    WorkflowPersonDto,
    WorkflowReadLogEntryDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';

/** Which of the three lists is on show. */
export type WorkflowParticipantsTab = 'approvers' | 'readLog' | 'delegates';

/**
 * Who may act on a request, who has acted, who stands in for whom, and who has merely looked.
 *
 * <p><b>Why this exists beside the conversation rather than instead of it.</b> An earlier draft of the
 * request dialog carried legacy's five-tab "Workflow Actions &amp; Information" panel; it was removed on the
 * argument that the single activity stream told the same story. It does not. The stream says what HAPPENED
 * -- submitted, approved, commented. It cannot say who is ENTITLED to act but has not yet, nor who is
 * covering for whom, nor who has opened the request without doing anything. Those are the questions an
 * approver actually asks before deciding, and the user asked for them back by name.</p>
 *
 * <p><b>Three tabs, not legacy's five.</b> Legacy's Request Log tab is every <c>RequestLog</c> row as a
 * grid, and the conversation beside this panel already renders every one of them as a sentence -- a second
 * copy in columns would be the one genuinely redundant surface. Legacy's Workflow Request tab is four
 * header fields dressed up as a one-row grid; they are shown here as a plain line above the tabs, where
 * they cost nothing and need no click.</p>
 *
 * <p>Deliberately quiet. This is reference material an approver consults, not the thing they came to do:
 * the decision buttons must stay the loudest element on the screen.</p>
 */
@Component({
    selector: 'workflow-participants-panel',
    templateUrl: './workflow-participants-panel.component.html',
    styleUrls: ['./workflow-participants-panel.component.less'],
    imports: [CommonModule, LocalizePipe, EmployeeAvatarComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowParticipantsPanelComponent extends AppComponentBase {
    private readonly _activityService = inject(WorkflowActivityServiceProxy);

    readonly workflowRequestId = input.required<number>();

    readonly loading = signal(false);
    readonly participants = signal<WorkflowParticipantsDto | undefined>(undefined);
    readonly tab = signal<WorkflowParticipantsTab>('approvers');

    readonly Origin = WorkflowApproverOrigin;
    readonly Relation = WorkflowDelegationRelation;

    readonly approvers = computed(() => this.participants()?.approvers ?? []);
    readonly readLog = computed(() => this.participants()?.readLog ?? []);
    readonly delegates = computed(() => this.participants()?.delegates ?? []);

    /**
     * Whether any row on the Approvers tab was rebuilt from the action trail rather than read from the
     * engine's own approver table -- which is the only case where the list can be incomplete, and so the
     * only case worth warning about. See the note on the server's <c>Origin</c> field.
     */
    readonly hasReconstructedApprovers = computed(() =>
        this.approvers().some((a) => a.origin === WorkflowApproverOrigin.ReconstructedFromLog)
    );

    readonly statusLabel = computed(() => {
        const status = this.participants()?.requestStatus;

        return status === undefined || status === null ? '' : this.l('WorkflowRequestStatus_' + RequestStatus[status]);
    });

    readonly currentWorkflowRole = computed(() =>
        this.pick(this.participants()?.currentWorkflowRoleNameEn, this.participants()?.currentWorkflowRoleNameAr)
    );

    constructor() {
        super();

        // Reloads whenever the host points the panel at a different request, which is what happens when the
        // dialog is reused for the next row in the inbox rather than destroyed between requests.
        effect(() => this.load(this.workflowRequestId()));
    }

    /** "1042 - Ahmad Al Sayed", or just the name when there is no code. */
    displayName(person: WorkflowPersonDto | undefined): string {
        if (!person) {
            return '';
        }

        return [person.code, this.pick(person.nameEn, person.nameAr)].filter((part) => !!part).join(' - ');
    }

    jobTitle(person: WorkflowPersonDto | undefined): string {
        return person ? this.pick(person.jobTitleEn, person.jobTitleAr) : '';
    }

    /**
     * What this approver has done, as one short phrase.
     *
     * <p>A step that was REJECTED or SENT BACK must not read as "has not got round to it yet", which is
     * what a bare is-approved flag would say about it.</p>
     */
    approverStatusLabel(entry: WorkflowApproverEntryDto): string {
        if (entry.isApproved) {
            return this.l('Approved');
        }

        if (entry.actionType !== undefined && entry.actionType !== null) {
            return this.l('WorkflowAction_' + RequestActionType[entry.actionType]);
        }

        return this.l('Pending');
    }

    /** Colour follows meaning, from the theme palette so it holds up in dark mode and in all 13 themes. */
    approverStatusClass(entry: WorkflowApproverEntryDto): string {
        if (entry.isApproved) {
            return 'wf-parts__pill--approved';
        }

        if (entry.actionType === RequestActionType.Rejected) {
            return 'wf-parts__pill--rejected';
        }

        if (
            entry.actionType === RequestActionType.SentBack ||
            entry.actionType === RequestActionType.SentBackToCreator
        ) {
            return 'wf-parts__pill--sentback';
        }

        return 'wf-parts__pill--pending';
    }

    /**
     * Who this delegation is on behalf of, phrased from the reader's side of it.
     *
     * <p>The two directions are genuinely different facts. "Cover for X" means one of this request's
     * approvers handed their authority away and the person named in the row is the one who will act.
     * "On this request in place of X" means the approver in the row is themselves a stand-in -- which is
     * the answer to "why is this person on my request at all?", and under HCMv3's substitution rule it is
     * the commoner of the two.</p>
     */
    delegationLabel(entry: WorkflowDelegateEntryDto): string {
        const other = this.displayName(entry.delegatedBy) || '-';

        return entry.relation === WorkflowDelegationRelation.ApproverDelegatedOut
            ? this.l('CoverFor', other)
            : this.l('OnThisRequestInPlaceOf', other);
    }

    readLogWho(entry: WorkflowReadLogEntryDto): string {
        // The raw stored value is the fallback, not a blank line: a read by somebody whose employee row has
        // since gone still happened, and saying so is better than dropping the row.
        return this.displayName(entry.person) || entry.user || '-';
    }

    dateTime(value: DateTime | undefined): string {
        return value?.toFormat('dd MMM yyyy HH:mm') ?? '';
    }

    dateOnly(value: DateTime | undefined): string {
        return value?.toFormat('dd MMM yyyy') ?? '';
    }

    private load(workflowRequestId: number): void {
        if (!workflowRequestId) {
            this.participants.set(undefined);
            return;
        }

        this.loading.set(true);

        const input = new GetWorkflowParticipantsInput();
        input.workflowRequestId = workflowRequestId;

        this._activityService
            .getParticipants(input)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.participants.set(result),

                // A refused or failed read must not take the dialog down with it -- the form, the decision
                // buttons and the conversation are all still usable, and the interceptor has already said
                // why this one failed.
                error: () => this.participants.set(undefined),
            });
    }

    private pick(en: string | undefined, ar: string | undefined): string {
        return this.isRtl ? ar || en || '' : en || '';
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
