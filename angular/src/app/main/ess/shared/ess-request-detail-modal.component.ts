import { ChangeDetectionStrategy, Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { CommonModule, NgComponentOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EmployeeAvatarComponent } from '@shared/common/employee-avatar/employee-avatar.component';
import { WorkflowActivityRailComponent } from '@app/shared/common/workflow-activity/workflow-activity-rail.component';
import { WorkflowParticipantsPanelComponent } from '@app/shared/common/workflow-participants/workflow-participants-panel.component';
import { WorkflowParticipantProfileModalComponent } from '@app/shared/common/workflow-participants/workflow-participant-profile-modal.component';
import {
    GetWorkflowRequestDetailInput,
    WorkflowActivityServiceProxy,
    WorkflowDetailFieldDto,
    WorkflowDetailFieldKind,
    WorkflowRequestDetailDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { findRequestForm } from './ess-request-forms';

/** One read-only field on the Request Information card. */
export interface EssRequestDetailField {
    label: string;
    value: string;
}

/** The three decisions an approver can reach from this dialog. */
export type EssRequestAction = 'approve' | 'reject' | 'sendBack';

/** What the host is told when the approver decides. */
export interface EssRequestDecision {
    action: EssRequestAction;
    comment: string;
    workflowRequestId: number;
}

/**
 * Everything the modal needs about a request.
 *
 * <p>Fields stay OPTIONAL and stay supported: the twenty-one self-service screens already hold the row the
 * employee clicked and pass it straight in, and making them wait for a round trip to show what they already
 * have would be a regression. What changed is that a request with a workflow id also asks the server for its
 * own handler's projection, which is the only way the approver's side can see fields nobody passed it.</p>
 */
export interface EssRequestDetailConfig {
    title: string;

    /** What the host already knows. Used as-is when the server has nothing better. */
    fields?: EssRequestDetailField[];

    /**
     * The note the employee typed when submitting. Shown as the first post in the conversation rather than
     * in a box of its own -- it is still a field on the request and still edited on the create form, only
     * its display moved.
     */
    comments?: string;

    /** Who to attribute that first post to. Falls back to whoever created the request. */
    requesterName?: string;
    requesterPhotoUrl?: string;

    workflowRequestId?: number;
    statusLabel: string;
    statusClass: string;

    /**
     * Turns on approver mode: the question at the top, the subject's card, and the decision panel. The host
     * lists only the actions the step actually permits, and the server re-checks every one of them.
     */
    actions?: EssRequestAction[];
}

/**
 * A submitted request, read-only, with its conversation -- and, for whoever it is waiting on, the decision.
 *
 * <p><b>Why this component has an approver mode at all.</b> It used not to, and the inbox worked around that
 * by docking its own decision bar to the bottom of the WINDOW while this dialog floated above it. The user's
 * verdict on the result was "approver screen is not correct at all, he should see same fields details as a
 * creator sees". Two things were wrong and they had the same cause. The bar was outside the dialog because
 * the dialog had no mode to put it in, and -- much worse -- the fields the inbox could pass in were only the
 * six the INBOX knows (employee, type, number, date, step, summary), because no endpoint existed that could
 * return a transaction's own fields. An approver was deciding on a one-line summary while the requester had
 * filled in a form.</p>
 *
 * <p>So the dialog now fetches <c>GetRequestDetail</c>, which asks the request's own workflow handler to
 * project its row, and renders the decision inside itself. The layout follows the SuccessFactors approval
 * screen the user sent: the question first, the request identified in one line, every field of the
 * transaction read-only, the subject as a person in a column of their own, and the activity below.</p>
 */
@Component({
    selector: 'essRequestDetailModal',
    templateUrl: './ess-request-detail-modal.component.html',
    styleUrls: ['./ess-request-detail-modal.component.less'],
    imports: [
        CommonModule,
        NgComponentOutlet,
        FormsModule,
        AppBsModalDirective,
        LocalizePipe,
        EmployeeAvatarComponent,
        WorkflowActivityRailComponent,
        WorkflowParticipantsPanelComponent,
        WorkflowParticipantProfileModalComponent,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EssRequestDetailModalComponent extends AppComponentBase {
    private readonly _activityService = inject(WorkflowActivityServiceProxy);

    readonly modal = viewChild<ModalDirective>('essRequestDetailModal');

    /** Raised when the approver decides. The host owns the call, because it also owns the list to refresh. */
    readonly decided = output<EssRequestDecision>();

    readonly active = signal(false);
    readonly config = signal<EssRequestDetailConfig | undefined>(undefined);
    readonly detail = signal<WorkflowRequestDetailDto | undefined>(undefined);
    readonly loadingDetail = signal(false);

    /** The comment attached to the decision. Distinct from the conversation composer, which posts a remark. */
    decisionComment = '';

    /** True while the host's approve/reject/send-back call is in flight. */
    readonly acting = signal(false);

    readonly Kind = WorkflowDetailFieldKind;

    readonly isApproverMode = computed(() => (this.config()?.actions?.length ?? 0) > 0);

    /**
     * The server's projection wins when it is the handler's own; otherwise whatever the host passed wins;
     * and the engine's three-field fallback is used only when there is nothing else at all.
     *
     * <p>This ordering is the whole fix. The inbox passes nothing, so an approver gets the handler's fields.
     * A self-service screen passes its own, so it keeps rendering instantly from the row already in hand.</p>
     */
    readonly fields = computed<EssRequestDetailField[]>(() => {
        const served = this.detail();
        const hosted = this.config()?.fields ?? [];

        if (served && served.fields?.length && !served.isDefaultProjection) {
            return served.fields.map((f) => ({ label: f.label ?? '', value: this.formatValue(f) }));
        }

        if (hosted.length) {
            return hosted;
        }

        return (served?.fields ?? []).map((f) => ({ label: f.label ?? '', value: this.formatValue(f) }));
    });

    /** True when all we could show is the engine's fallback, so the card can say so rather than imply. */
    readonly showingFallbackOnly = computed(
        () => !!this.detail()?.isDefaultProjection && !(this.config()?.fields?.length ?? 0)
    );

    /**
     * Every projected field keyed by its stable name, which is what a typed form binds on.
     *
     * <p>Keyed on <c>name</c> and never on <c>label</c>: the label is localized, so a lookup by label would
     * quietly return nothing the moment the viewer switched to Arabic, and the form would render empty with
     * no error to explain it.</p>
     */
    private readonly fieldValues = computed<Record<string, string>>(() => {
        const map: Record<string, string> = {};

        for (const field of this.detail()?.fields ?? []) {
            if (field.name) {
                map[field.name] = field.value ?? '';
            }
        }

        return map;
    });

    /**
     * The request type's OWN form, when it has one -- the real create form, read-only.
     *
     * <p>This is what the user asked for in place of a generic list: "same layout same screen at the time of
     * request create". A type without a registered form is not left worse off; it falls back to the
     * projected label/value rows, which is still every field its handler publishes.</p>
     *
     * <p>Waits for the detail to arrive. Rendering the form against an empty projection would show a
     * complete, correctly laid out, entirely blank permit, which reads as data loss rather than as
     * loading.</p>
     */
    readonly requestForm = computed(() => {
        const served = this.detail();
        if (!served || served.isDefaultProjection) {
            return undefined;
        }

        const registration = findRequestForm(served.requestType);
        if (!registration) {
            return undefined;
        }

        return {
            component: registration.component,
            inputs: {
                mode: this.isApproverMode() ? 'approve' : 'view',
                value: registration.toValue({
                    values: this.fieldValues(),
                    employeeDisplay: this.employeeDisplay(),
                }),
            } as Record<string, unknown>,
        };
    });

    /** The name on its own, with no code attached either side of it. */
    readonly requesterPlainName = computed(() => {
        const served = this.detail();
        const fromServer = this.pick(served?.requesterNameEn, served?.requesterNameAr);

        return fromServer || this.config()?.requesterName || '';
    });

    /**
     * "1042 - Ahmad Al Sayed" for the form's Employee field, which puts the code in FRONT.
     *
     * Built from the plain name on purpose: requesterName() puts the code at the end, and feeding that in
     * here produced "2362 - Shehata Kamal Seliman Awad (2362)" -- the same code at both ends of one field.
     */
    readonly employeeDisplay = computed(() =>
        [this.detail()?.requesterCode, this.requesterPlainName()].filter((part) => !!part).join(' - ')
    );

    /**
     * "Ahmad Al Sayed (1042)" -- the form used everywhere a PERSON is named rather than a field filled:
     * the conversation, the approvers list, the "Initiated by" line. Two colleagues can share a name and
     * the code is what separates them, so all three say it the same way.
     */
    readonly requesterName = computed(() => {
        const name = this.requesterPlainName();
        const code = this.detail()?.requesterCode;

        return name && code ? `${name} (${code})` : name;
    });

    readonly requesterJobTitle = computed(() =>
        this.pick(this.detail()?.requesterJobTitleEn, this.detail()?.requesterJobTitleAr)
    );

    readonly requesterPhotoUrl = computed(
        () => this.detail()?.requesterPhotoUrl ?? this.config()?.requesterPhotoUrl
    );

    readonly typeName = computed(() => {
        const served = this.detail();
        return this.pick(served?.requestTypeNameEn, served?.requestTypeNameAr) || this.config()?.title || '';
    });

    /** "Initiated by Ahmed Al-Sayed on 21 Sep 2026", the one line that identifies the request. */
    readonly initiatedBy = computed(() => {
        const name = this.requesterName();
        const on = this.detail()?.submittedOn;

        if (!name) {
            return '';
        }

        return this.l('InitiatedBy', name, on ? on.toFormat('dd MMM yyyy') : '');
    });

    /**
     * The actions to offer. The host says which the step permits; the server says whether this viewer is
     * still the approver it is waiting on. Both must agree, so a request approved in another tab stops
     * offering buttons as soon as the dialog reloads.
     */
    readonly availableActions = computed<EssRequestAction[]>(() => {
        const requested = this.config()?.actions ?? [];
        const served = this.detail();

        if (!requested.length) {
            return [];
        }

        // Before the detail arrives, trust the host -- it got these from the inbox, which asked the same
        // engine. Hiding the buttons for a moment and then showing them reads as a glitch.
        if (!served) {
            return requested;
        }

        if (!served.isCurrentApprover) {
            return [];
        }

        return requested.filter((action) => {
            if (action === 'approve') return served.canApprove;
            if (action === 'reject') return served.canReject;
            return served.canSendBack;
        });
    });

    show(config: EssRequestDetailConfig): void {
        this.config.set(config);
        this.detail.set(undefined);
        this.decisionComment = '';
        this.acting.set(false);
        this.active.set(true);
        this.modal()?.show();

        if (config.workflowRequestId) {
            this.loadDetail(config.workflowRequestId);
        }
    }

    close(): void {
        this.active.set(false);
        this.config.set(undefined);
        this.detail.set(undefined);
        this.decisionComment = '';
        this.acting.set(false);
        this.modal()?.hide();
    }

    /** Lets the host show the dialog as busy while its own approve/reject call runs. */
    setActing(acting: boolean): void {
        this.acting.set(acting);
    }

    decide(action: EssRequestAction): void {
        const requestId = this.config()?.workflowRequestId;
        if (!requestId || this.acting()) {
            return;
        }

        const comment = (this.decisionComment || '').trim();

        // Told here rather than after a round trip. The server requires a reason for both of these, and an
        // approver who has just typed nothing should learn that before the request is sent, not after.
        if ((action === 'reject' || action === 'sendBack') && !comment) {
            this.message.warn(this.l(action === 'reject' ? 'AReasonIsRequiredToReject' : 'AReasonIsRequiredToSendBack'));
            return;
        }

        this.decided.emit({ action, comment, workflowRequestId: requestId });
    }

    /**
     * Renders a transport value in the viewer's own terms.
     *
     * <p>The server sends dates as ISO and numbers as invariant precisely so this can happen here: the
     * browser knows the viewer's locale and the server does not. A kind it has never heard of falls through
     * to the raw string, which is always readable even when it is not pretty -- a new field kind should
     * never blank out an approver's screen.</p>
     */
    formatValue(field: WorkflowDetailFieldDto): string {
        const raw = field.value;
        if (raw === undefined || raw === null || raw === '') {
            return '';
        }

        switch (field.kind) {
            case WorkflowDetailFieldKind.Date:
                return this.formatDate(raw, 'dd MMM yyyy');
            case WorkflowDetailFieldKind.DateTime:
                return this.formatDate(raw, 'dd MMM yyyy HH:mm');
            case WorkflowDetailFieldKind.Number:
                return this.formatNumber(raw, 0, 2);
            case WorkflowDetailFieldKind.Money:
                return this.formatNumber(raw, 3, 3);
            case WorkflowDetailFieldKind.Boolean:
                return this.l(raw === 'true' ? 'Yes' : 'No');
            default:
                return raw;
        }
    }

    private formatDate(raw: string, format: string): string {
        const parsed = DateTime.fromISO(raw);

        return parsed.isValid ? parsed.toFormat(format) : raw;
    }

    private formatNumber(raw: string, min: number, max: number): string {
        const parsed = Number(raw);

        return Number.isFinite(parsed)
            ? parsed.toLocaleString(undefined, { minimumFractionDigits: min, maximumFractionDigits: max })
            : raw;
    }

    private loadDetail(requestId: number): void {
        this.loadingDetail.set(true);

        const input = new GetWorkflowRequestDetailInput();
        input.workflowRequestId = requestId;

        this._activityService
            .getRequestDetail(input)
            .pipe(finalize(() => this.loadingDetail.set(false)))
            .subscribe({
                next: (result) => this.detail.set(result),

                // A refused or failed detail read must not blank the dialog: the conversation and whatever
                // the host passed in are still worth showing, and the interceptor has already said why.
                error: () => this.detail.set(undefined),
            });
    }

    private pick(en: string | undefined, ar: string | undefined): string {
        return this.isRtl ? ar || en || '' : en || '';
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
