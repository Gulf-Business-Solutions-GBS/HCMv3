import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditPARatingDto, PARatingsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPARatingModal',
    templateUrl: './create-or-edit-pa-rating-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPARatingModalComponent extends AppComponentBase {
    private _service = inject(PARatingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly paRating = signal<CreateOrEditPARatingDto>(new CreateOrEditPARatingDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditPARatingDto();
            record.status = true;
            this.paRating.set(record);
            this.modal().show();
        } else {
            this._service.getPARatingForEdit(id).subscribe((result) => {
                this.paRating.set(result.paRating);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PARating_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.paRating())
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

    updateField(field: keyof CreateOrEditPARatingDto, value: any): void {
        const current = this.paRating();
        (current as any)[field] = value;
        const next = new CreateOrEditPARatingDto();
        Object.assign(next, current);
        this.paRating.set(next);
    }
}
