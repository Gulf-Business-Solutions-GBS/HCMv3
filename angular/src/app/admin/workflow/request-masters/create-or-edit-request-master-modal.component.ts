import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRequestMasterDto,
    RequestMastersServiceProxy,
    WorkflowSecurityLevel,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { RequestMasterFormComponent } from './request-master-form.component';

/**
 * Creates a chain. Editing one happens on its own page, where its steps are, so there is only ever one place
 * a chain's fields can be changed.
 */
@Component({
    selector: 'createOrEditRequestMasterModal',
    templateUrl: './create-or-edit-request-master-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe, RequestMasterFormComponent],
})
export class CreateOrEditRequestMasterModalComponent extends AppComponentBase {
    private _service = inject(RequestMastersServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditRequestMasterDto>(new CreateOrEditRequestMasterDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.record.set(new CreateOrEditRequestMasterDto({ securityLevel: WorkflowSecurityLevel.EmployeeOnly } as any));
            this.modal().show();
        } else {
            this._service.getRequestMasterForEdit(id).subscribe((result) => {
                this.record.set(result.requestMaster);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RequestMaster_Code')?.focus();
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
}
