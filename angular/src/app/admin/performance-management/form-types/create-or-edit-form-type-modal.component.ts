import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditFormTypeDto, FormTypesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditFormTypeModal',
    templateUrl: './create-or-edit-form-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditFormTypeModalComponent extends AppComponentBase {
    private _service = inject(FormTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly formType = signal<CreateOrEditFormTypeDto>(new CreateOrEditFormTypeDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.formType.set(new CreateOrEditFormTypeDto());
            this.modal().show();
        } else {
            this._service.getFormTypeForEdit(id).subscribe((result) => {
                this.formType.set(result.formType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('FormType_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.formType())
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

    updateField(field: keyof CreateOrEditFormTypeDto, value: any): void {
        const current = this.formType();
        (current as any)[field] = value;
        const next = new CreateOrEditFormTypeDto();
        Object.assign(next, current);
        this.formType.set(next);
    }
}
