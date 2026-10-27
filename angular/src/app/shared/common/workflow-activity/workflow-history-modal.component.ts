import { Component, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { WorkflowActivityRailComponent } from './workflow-activity-rail.component';

/**
 * Read-only view of a transaction's approval history, opened from the transaction itself.
 *
 * <p>This is the whole point of keeping the request and the transaction in one row. In legacy the approved
 * leave lived in EmployeeLeave and its approval history in a separate LeaveRequest, with no link between
 * them -- an HR user looking at a leave record could not see who approved it or why. Here the row carries
 * WorkflowRequestId, so the same activity rail used on the approval screen opens straight from the leave
 * line.</p>
 *
 * <p>Read-only on purpose: this is a history view. Commenting belongs on the request itself, where the
 * people in the conversation are looking.</p>
 */
@Component({
    selector: 'workflowHistoryModal',
    templateUrl: './workflow-history-modal.component.html',
    imports: [AppBsModalDirective, LocalizePipe, WorkflowActivityRailComponent],
})
export class WorkflowHistoryModalComponent extends AppComponentBase {
    readonly modal = viewChild<ModalDirective>('workflowHistoryModal');

    readonly active = signal(false);
    readonly workflowRequestId = signal<number | undefined>(undefined);

    show(workflowRequestId: number): void {
        this.workflowRequestId.set(workflowRequestId);
        this.active.set(true);
        this.modal()?.show();
    }

    close(): void {
        this.active.set(false);
        this.workflowRequestId.set(undefined);
        this.modal()?.hide();
    }
}
