import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApproveWorkflowRequestDto,
    ReassignWorkflowRequestDto,
    RejectWorkflowRequestDto,
    SendBackWorkflowRequestDto,
    WorkflowRequestsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * Shared action type driving this modal's title/localization key and which
 * WorkflowRequestsServiceProxy method it calls on save. Reassign additionally collects the
 * new approver's Employee id.
 */
export type WorkflowRequestActionType = 'Approve' | 'Reject' | 'SendBack' | 'Reassign';

@Component({
    selector: 'workflowRequestRemarkModal',
    templateUrl: './workflow-request-remark-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class WorkflowRequestRemarkModalComponent extends AppComponentBase {
    private _service = inject(WorkflowRequestsServiceProxy);

    readonly modal = viewChild<ModalDirective>('actionModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly action = signal<WorkflowRequestActionType>('Approve');
    readonly remark = signal<string>('');
    readonly newApproverEmployeeId = signal<number | undefined>(undefined);

    private _requestId: number;

    show(requestId: number, action: WorkflowRequestActionType): void {
        this._requestId = requestId;
        this.action.set(action);
        this.remark.set('');
        this.newApproverEmployeeId.set(undefined);
        this.active.set(true);
        this.modal().show();
    }

    get titleKey(): string {
        switch (this.action()) {
            case 'Approve':
                return 'Approve';
            case 'Reject':
                return 'Reject';
            case 'SendBack':
                return 'SendBack';
            case 'Reassign':
                return 'Reassign';
        }
    }

    save(): void {
        this.saving.set(true);

        let observable;
        switch (this.action()) {
            case 'Approve':
                observable = this._service.approveWorkflowRequest(
                    new ApproveWorkflowRequestDto({ requestId: this._requestId, remark: this.remark() })
                );
                break;
            case 'Reject':
                observable = this._service.rejectWorkflowRequest(
                    new RejectWorkflowRequestDto({ requestId: this._requestId, remark: this.remark() })
                );
                break;
            case 'SendBack':
                observable = this._service.sendBackWorkflowRequest(
                    new SendBackWorkflowRequestDto({ requestId: this._requestId, remark: this.remark() })
                );
                break;
            case 'Reassign':
                observable = this._service.reassignWorkflowRequest(
                    new ReassignWorkflowRequestDto({
                        requestId: this._requestId,
                        newApproverEmployeeId: this.newApproverEmployeeId(),
                        remark: this.remark(),
                    })
                );
                break;
        }

        observable.pipe(finalize(() => this.saving.set(false))).subscribe(() => {
            this.notify.success(this.l('SavedSuccessfully'));
            this.close();
            this.modalSave.emit();
        });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
