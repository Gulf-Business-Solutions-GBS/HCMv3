import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeAvatarComponent } from '@shared/common/employee-avatar/employee-avatar.component';
import {
    ApprovalRequire,
    GetWorkflowParticipantsInput,
    GetWorkflowRequestLogInput,
    WorkflowPlatform,
    WorkflowRequestLogEntryDto,
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
import { WorkflowParticipantProfileModalComponent } from './workflow-participant-profile-modal.component';

/** Which of the three lists is on show. */
export type WorkflowParticipantsTab = 'approvers' | 'readLog' | 'delegates' | 'log';

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
    imports: [CommonModule, LocalizePipe, EmployeeAvatarComponent, WorkflowParticipantProfileModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkflowParticipantsPanelComponent extends AppComponentBase {
    private readonly _activityService = inject(WorkflowActivityServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    readonly workflowRequestId = input.required<number>();

    readonly profileModal = viewChild<WorkflowParticipantProfileModalComponent>('profileModal');

    readonly loading = signal(false);
    readonly participants = signal<WorkflowParticipantsDto | undefined>(undefined);
    readonly tab = signal<WorkflowParticipantsTab>('approvers');

    // ---------------------------------------------------------------- log

    /**
     * The action trail, as a tab of this panel rather than a toggle on the conversation card.
     *
     * <p>It belongs here because this panel is where a reader already comes for the mechanics of the
     * request -- who is entitled to act, who has looked, who is standing in. The log is the fourth answer
     * of that set, and having it live somewhere else meant two different places to look for one story.</p>
     */
    readonly logEntries = signal<WorkflowRequestLogEntryDto[]>([]);
    readonly loadingLog = signal(false);
    readonly exportingLog = signal(false);

    readonly Origin = WorkflowApproverOrigin;
    readonly Relation = WorkflowDelegationRelation;

    readonly approvers = computed(() => this.participants()?.approvers ?? []);
    readonly readLog = computed(() => this.participants()?.readLog ?? []);
    readonly delegates = computed(() => this.participants()?.delegates ?? []);

    /**
     * The approvers split into their steps, in order, each carrying the rule that governs it.
     *
     * <p>One flat list with a single rule above it was the bug: "any one of these approvers is enough"
     * described step 3 while sitting above step 2 as well. A chain is a sequence of steps and reads as
     * one.</p>
     */
    readonly approverSteps = computed(() => {
        const byStep = new Map<number, WorkflowApproverEntryDto[]>();

        for (const entry of this.approvers()) {
            const step = entry.workflowStep ?? 0;
            const group = byStep.get(step);

            if (group) {
                group.push(entry);
            } else {
                byStep.set(step, [entry]);
            }
        }

        return [...byStep.entries()]
            .sort((a, b) => a[0] - b[0])
            .map(([step, entries]) => ({
                step,
                entries,
                isCreatorStep: entries.every((e) => e.origin === WorkflowApproverOrigin.CreatorStep),

                // Stated only where it tells the reader something. One approver means there is no choice
                // to describe -- "any one of these approvers is enough" above a list of one is noise --
                // and a creator step has no approval rule at all.
                rule:
                    entries.length > 1 && entries.some((e) => e.origin !== WorkflowApproverOrigin.CreatorStep)
                        ? this.ruleLabel(entries[0].approvalRequire)
                        : '',
            }));
    });

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

    /**
     * "Any one of these approvers is enough" / "All of these approvers must approve", for one step.
     *
     * <p>Empty for a rule the server could not resolve, so the panel says nothing rather than asserting a
     * default -- claiming the wrong rule is worse than claiming none.</p>
     */
    private ruleLabel(rule: ApprovalRequire | undefined): string {
        switch (rule) {
            case ApprovalRequire.Single:
                return this.l('ApprovalRuleSingle');
            case ApprovalRequire.All:
                return this.l('ApprovalRuleAll');
            case ApprovalRequire.Parallel:
                return this.l('ApprovalRuleParallel');
            default:
                return '';
        }
    }

    readonly currentWorkflowRole = computed(() =>
        this.pick(this.participants()?.currentWorkflowRoleNameEn, this.participants()?.currentWorkflowRoleNameAr)
    );

    constructor() {
        super();

        // Reloads whenever the host points the panel at a different request, which is what happens when the
        // dialog is reused for the next row in the inbox rather than destroyed between requests.
        effect(() => this.load(this.workflowRequestId()));
    }

    /**
     * "Karam Ismaeil (3804)" -- name first, code in brackets after it.
     *
     * <p>Legacy showed Code and Approver as two separate grid columns. The user asked for this shape
     * instead, and it is the better one for a narrow right-hand column: the name is what identifies the
     * person, and leading with a number pushed it out of sight on every row.</p>
     */
    displayName(person: WorkflowPersonDto | undefined): string {
        if (!person) {
            return '';
        }

        const name = this.plainName(person);

        if (!name) {
            return person.code ?? '';
        }

        return person.code ? `${name} (${person.code})` : name;
    }

    /**
     * The name alone, for the avatar.
     *
     * <p>Separate from <see cref="displayName"/> on purpose: the avatar derives initials from whatever it
     * is given, and "Karam Ismaeil (3804)" would have it drawing "K(" for anyone without a photo.</p>
     */
    plainName(person: WorkflowPersonDto | undefined): string {
        return person ? this.pick(person.nameEn, person.nameAr) : '';
    }

    /**
     * Whether this name should be offered as a link.
     *
     * <p>A row with no resolved employee -- a read-log entry whose stored user no longer matches anybody --
     * has no card to open, and a link that goes nowhere is worse than plain text.</p>
     */
    canOpenProfile(person: WorkflowPersonDto | undefined): boolean {
        return !!person?.employeeId;
    }

    openProfile(person: WorkflowPersonDto | undefined): void {
        // Read the id out first rather than leaning on canOpenProfile to narrow the type for us -- narrowing
        // does not cross a function boundary, and this stays correct if strictNullChecks is ever turned on.
        const employeeId = person?.employeeId;

        if (!employeeId) {
            return;
        }

        this.profileModal()?.show(this.workflowRequestId(), employeeId, this.plainName(person));
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
    /**
     * The word against a name, or nothing at all.
     *
     * <p>Nothing is a real answer here and the reason it exists: on a step where any one approval is
     * enough and somebody has approved, the others are not late. They are not being asked. "Pending"
     * against them is a request for an action that is never coming, and it is how one approver ends up
     * chasing a colleague for an approval nobody needs.</p>
     *
     * <p>The creator is likewise not pending anything -- they submitted it, which the step heading
     * already says.</p>
     */
    approverStatusLabel(entry: WorkflowApproverEntryDto): string {
        if (entry.origin === WorkflowApproverOrigin.CreatorStep) {
            return '';
        }

        if (entry.isApproved) {
            return this.l('Approved');
        }

        if (entry.actionType !== undefined && entry.actionType !== null) {
            return this.l('WorkflowAction_' + RequestActionType[entry.actionType]);
        }

        if (entry.isNotRequired) {
            return '';
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

    /**
     * Switches tab, fetching the log the first time it is asked for.
     *
     * <p>Lazily: most readers never open it, and a second read of the request's history on every dialog
     * open would cost a round trip per approval for a tab nobody pressed.</p>
     */
    selectTab(tab: WorkflowParticipantsTab): void {
        this.tab.set(tab);

        if (tab === 'log' && !this.logEntries().length && !this.loadingLog()) {
            this.loadLog();
        }
    }

    private loadLog(): void {
        const requestId = this.workflowRequestId();

        if (!requestId) {
            return;
        }

        this.loadingLog.set(true);

        const input = new GetWorkflowRequestLogInput();
        input.workflowRequestId = requestId;

        this._activityService
            .getRequestLog(input)
            .pipe(finalize(() => this.loadingLog.set(false)))
            .subscribe({
                next: (entries) => this.logEntries.set(entries ?? []),
                error: () => this.logEntries.set([]),
            });
    }

    exportLog(): void {
        const requestId = this.workflowRequestId();

        if (!requestId || this.exportingLog()) {
            return;
        }

        this.exportingLog.set(true);

        const input = new GetWorkflowRequestLogInput();
        input.workflowRequestId = requestId;

        this._activityService
            .exportRequestLogToExcel(input)
            .pipe(finalize(() => this.exportingLog.set(false)))
            .subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    /** The action's own word -- "Approved", "Sent back" -- rather than its number. */
    actionTypeLabel(actionType: RequestActionType): string {
        return this.l('RequestActionType_' + RequestActionType[actionType]);
    }

    /**
     * Likewise for the platform. Interpolating the enum straight into the key builds
     * "WorkflowPlatform_0", which matches nothing and renders as itself.
     */
    platformLabel(platform: WorkflowPlatform): string {
        return this.l('WorkflowPlatform_' + WorkflowPlatform[platform]);
    }

    /** Whose name to show on a log row, in the reader's language, with the code that disambiguates it. */
    logActorName(entry: WorkflowRequestLogEntryDto): string {
        const name = this.pick(entry.actionByNameEn, entry.actionByNameAr) || '';

        return name && entry.actionByCode ? `${name} (${entry.actionByCode})` : name;
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
