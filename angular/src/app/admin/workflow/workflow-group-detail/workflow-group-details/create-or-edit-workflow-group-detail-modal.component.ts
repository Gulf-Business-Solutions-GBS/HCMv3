import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWorkflowGroupDetailDto,
    GetAllRequestMastersInput,
    RequestMasterDto,
    RequestMastersServiceProxy,
    WorkflowGroupDetailsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditWorkflowGroupDetailModal',
    templateUrl: './create-or-edit-workflow-group-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWorkflowGroupDetailModalComponent extends AppComponentBase {
    private _service = inject(WorkflowGroupDetailsServiceProxy);
    private _requestMastersService = inject(RequestMastersServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditWorkflowGroupDetailDto>(new CreateOrEditWorkflowGroupDetailDto());
    readonly requestMasters = signal<RequestMasterDto[]>([]);

    show(workflowGroupId: number, id?: number): void {
        this.active.set(true);

        this._requestMastersService
            .getAll(new GetAllRequestMastersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.requestMasters.set(result.items));

        if (!id) {
            const record = new CreateOrEditWorkflowGroupDetailDto();
            record.workflowGroupId = workflowGroupId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getWorkflowGroupDetailForEdit(id).subscribe((result) => {
                this.record.set(result.workflowGroupDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WorkflowGroupDetail_RequestMasterId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateField(field: keyof CreateOrEditWorkflowGroupDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditWorkflowGroupDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
