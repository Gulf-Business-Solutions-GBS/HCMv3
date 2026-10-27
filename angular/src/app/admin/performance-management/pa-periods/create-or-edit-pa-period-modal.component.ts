import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditPAPeriodDto, PAPeriodsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPAPeriodModal',
    templateUrl: './create-or-edit-pa-period-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAPeriodModalComponent extends AppComponentBase {
    private _service = inject(PAPeriodsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly paPeriod = signal<CreateOrEditPAPeriodDto>(new CreateOrEditPAPeriodDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.paPeriod.set(new CreateOrEditPAPeriodDto());
            this.modal().show();
        } else {
            this._service.getPAPeriodForEdit(id).subscribe((result) => {
                this.paPeriod.set(result.paPeriod);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAPeriod_Name')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.paPeriod())
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

    updateField(field: keyof CreateOrEditPAPeriodDto, value: any): void {
        const current = this.paPeriod();
        (current as any)[field] = value;
        const next = new CreateOrEditPAPeriodDto();
        Object.assign(next, current);
        this.paPeriod.set(next);
    }
}
