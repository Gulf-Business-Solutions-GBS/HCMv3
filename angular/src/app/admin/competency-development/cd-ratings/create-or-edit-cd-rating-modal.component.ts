import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditCDRatingDto, CDRatingsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCDRatingModal',
    templateUrl: './create-or-edit-cd-rating-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCDRatingModalComponent extends AppComponentBase {
    private _service = inject(CDRatingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly cdRating = signal<CreateOrEditCDRatingDto>(new CreateOrEditCDRatingDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.cdRating.set(new CreateOrEditCDRatingDto());
            this.modal().show();
        } else {
            this._service.getCDRatingForEdit(id).subscribe((result) => {
                this.cdRating.set(result.cdRating);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CDRating_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.cdRating())
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

    updateField(field: keyof CreateOrEditCDRatingDto, value: any): void {
        const current = this.cdRating();
        (current as any)[field] = value;
        const next = new CreateOrEditCDRatingDto();
        Object.assign(next, current);
        this.cdRating.set(next);
    }
}
