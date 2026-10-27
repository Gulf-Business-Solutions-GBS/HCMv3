import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditOnbTemplateDto, OnbTemplatesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditOnbTemplateModal',
    templateUrl: './create-or-edit-onb-template-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditOnbTemplateModalComponent extends AppComponentBase {
    private _service = inject(OnbTemplatesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditOnbTemplateDto>(new CreateOrEditOnbTemplateDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.record.set(new CreateOrEditOnbTemplateDto());
            this.modal().show();
        } else {
            this._service.getOnbTemplateForEdit(id).subscribe((result) => {
                this.record.set(result.onbTemplate);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('OnbTemplate_TemplateTitle')?.focus();
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

    updateField(field: keyof CreateOrEditOnbTemplateDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditOnbTemplateDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
