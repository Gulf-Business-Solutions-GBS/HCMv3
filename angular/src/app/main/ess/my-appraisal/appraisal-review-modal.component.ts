import { ChangeDetectionStrategy, Component, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppraisalReviewFormComponent, AppraisalReviewFormValue } from './appraisal-review-form.component';

/**
 * The appraisal's Details dialog.
 *
 * <p><b>Its own dialog rather than the shared request-detail one, on purpose.</b> The shared dialog reads the
 * request through the workflow activity service, which admits only the requester, the approvers and people
 * asked for feedback. A peer reviewer is none of those -- they are on the appraisal's reviewer list, not on
 * the request -- so the shared dialog would refuse the very people who most need to open it. This dialog
 * hosts the same review form the approver's inbox shows, read through the appraisal service, which admits
 * reviewers.</p>
 */
@Component({
    selector: 'appraisalReviewModal',
    templateUrl: './appraisal-review-modal.component.html',
    imports: [AppBsModalDirective, LocalizePipe, AppraisalReviewFormComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppraisalReviewModalComponent extends AppComponentBase {
    readonly modal = viewChild<ModalDirective>('reviewModal');

    /** Something changed on the appraisal; the host's list should reload. */
    readonly changed = output<void>();

    readonly value = signal<AppraisalReviewFormValue | undefined>(undefined);
    readonly title = signal('');

    show(value: AppraisalReviewFormValue, title: string): void {
        this.title.set(title);
        this.value.set({ ...value });
        this.modal()?.show();
    }

    close(): void {
        this.modal()?.hide();
        this.value.set(undefined);
    }
}
