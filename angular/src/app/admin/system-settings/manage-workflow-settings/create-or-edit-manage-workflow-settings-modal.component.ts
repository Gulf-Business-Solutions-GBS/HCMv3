import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { WorkflowSettingsEditDto, ManageWorkflowSettingsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManageWorkflowSettingsModal',
    templateUrl: './create-or-edit-manage-workflow-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManageWorkflowSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageWorkflowSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<WorkflowSettingsEditDto>(new WorkflowSettingsEditDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new WorkflowSettingsEditDto();
            record.enableWorkflowGroup = false;
            record.enableOrgUnitTypeInWorkflow = false;
            record.hideLeavePayDetailsInWorkflow = false;
            record.enableTravelCostOnWorkflowstep = 0;
            record.overtimeRequestModificationStep = 0;
            record.dynamicWorkflowSteps = false;
            record.sendBackToCreator = false;
            record.delegateEmployeeListByOrganizationUnitTypeId = undefined;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getWorkflowSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WorkflowSettings_EnableWorkflowGroup')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditWorkflowSettings(this.record())
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

    updateField(field: keyof WorkflowSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new WorkflowSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof WorkflowSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }

    updateNullableNumberField(field: keyof WorkflowSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : undefined);
    }
}
