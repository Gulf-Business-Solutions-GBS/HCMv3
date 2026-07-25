import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditCDPeriodDto, CDPeriodsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCDPeriodModal',
    templateUrl: './create-or-edit-cd-period-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCDPeriodModalComponent extends AppComponentBase {
    private _service = inject(CDPeriodsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly cdPeriod = signal<CreateOrEditCDPeriodDto>(new CreateOrEditCDPeriodDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.cdPeriod.set(new CreateOrEditCDPeriodDto());
            this.modal().show();
        } else {
            this._service.getCDPeriodForEdit(id).subscribe((result) => {
                this.cdPeriod.set(result.cdPeriod);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CDPeriod_Name')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.cdPeriod())
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

    updateField(field: keyof CreateOrEditCDPeriodDto, value: any): void {
        const current = this.cdPeriod();
        (current as any)[field] = value;
        const next = new CreateOrEditCDPeriodDto();
        Object.assign(next, current);
        this.cdPeriod.set(next);
    }
}
