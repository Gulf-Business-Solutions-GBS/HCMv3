import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditSignatoryDto, SignatoriesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditSignatoryModal',
    templateUrl: './create-or-edit-signatory-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSignatoryModalComponent extends AppComponentBase {
    private _service = inject(SignatoriesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSignatoryDto>(new CreateOrEditSignatoryDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.record.set(new CreateOrEditSignatoryDto());
            this.modal().show();
        } else {
            this._service.getSignatoryForEdit(id).subscribe((result) => {
                this.record.set(result.signatory);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Signatory_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditSignatoryDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSignatoryDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
