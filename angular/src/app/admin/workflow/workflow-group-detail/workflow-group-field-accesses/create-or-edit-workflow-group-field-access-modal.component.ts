import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditWorkflowGroupFieldAccessDto, WorkflowGroupFieldAccessesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditWorkflowGroupFieldAccessModal',
    templateUrl: './create-or-edit-workflow-group-field-access-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWorkflowGroupFieldAccessModalComponent extends AppComponentBase {
    private _service = inject(WorkflowGroupFieldAccessesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditWorkflowGroupFieldAccessDto>(new CreateOrEditWorkflowGroupFieldAccessDto());

    show(workflowGroupDetailId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditWorkflowGroupFieldAccessDto();
            record.workflowGroupDetailId = workflowGroupDetailId;
            record.isVisible = true;
            record.isEditable = true;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getWorkflowGroupFieldAccessForEdit(id).subscribe((result) => {
                this.record.set(result.workflowGroupFieldAccess);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WorkflowGroupFieldAccess_Step')?.focus();
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

    updateField(field: keyof CreateOrEditWorkflowGroupFieldAccessDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditWorkflowGroupFieldAccessDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
