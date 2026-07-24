import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditPACycleDto, PACyclesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPACycleModal',
    templateUrl: './create-or-edit-pa-cycle-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPACycleModalComponent extends AppComponentBase {
    private _service = inject(PACyclesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly paCycle = signal<CreateOrEditPACycleDto>(new CreateOrEditPACycleDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.paCycle.set(new CreateOrEditPACycleDto());
            this.modal().show();
        } else {
            this._service.getPACycleForEdit(id).subscribe((result) => {
                this.paCycle.set(result.paCycle);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PACycle_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.paCycle())
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

    updateField(field: keyof CreateOrEditPACycleDto, value: any): void {
        const current = this.paCycle();
        (current as any)[field] = value;
        const next = new CreateOrEditPACycleDto();
        Object.assign(next, current);
        this.paCycle.set(next);
    }
}
