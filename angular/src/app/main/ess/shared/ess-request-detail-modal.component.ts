import { ChangeDetectionStrategy, Component, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { WorkflowActivityRailComponent } from '@app/shared/common/workflow-activity/workflow-activity-rail.component';
import { ModalDirective } from 'ngx-bootstrap/modal';

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
}

/**
 * A submitted self-service request, opened read-only, with its conversation.
 *
 * <p>Fills the gap the user found: the list showed a status badge and a withdraw link, so an employee could
 * see THAT their request was waiting but never WHAT they had submitted.</p>
 *
 * <p>Two cards, not legacy's four. The legacy form paired a "Request Comment" textarea with a read-only
 * "Workflow Comment" one and put the action trail in a tabbed grid below; the user rejected that layout
 * twice, asking for SuccessFactors' single threaded stream. So the comments, the approvals and the reads all
 * render as one conversation, and the tabs -- which only ever restated the same rows as columns -- are
 * gone.</p>
 *
 * <p>The request fields stay read-only: once a request is in the chain the workflow owns the row, and the
 * only thing the requester can still do is withdraw it, which stays on the list where it was. Commenting is
 * the exception, and the server decides whether this viewer may.</p>
 */
@Component({
    selector: 'essRequestDetailModal',
    templateUrl: './ess-request-detail-modal.component.html',
    imports: [CommonModule, AppBsModalDirective, LocalizePipe, WorkflowActivityRailComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EssRequestDetailModalComponent extends AppComponentBase {
    readonly modal = viewChild<ModalDirective>('essRequestDetailModal');

    readonly active = signal(false);
    readonly config = signal<EssRequestDetailConfig | undefined>(undefined);

    show(config: EssRequestDetailConfig): void {
        this.config.set(config);
        this.active.set(true);
        this.modal()?.show();
    }

    close(): void {
        this.active.set(false);
        this.config.set(undefined);
        this.modal()?.hide();
    }
}
