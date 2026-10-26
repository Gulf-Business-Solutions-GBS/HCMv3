import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWageTypeRegisterDto,
    WageTypeRegistersServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditWageTypeRegisterModal',
    templateUrl: './create-or-edit-wage-type-register-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWageTypeRegisterModalComponent extends AppComponentBase {
    private _wageTypeRegistersService = inject(WageTypeRegistersServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly wageTypeRegister = signal<CreateOrEditWageTypeRegisterDto>(new CreateOrEditWageTypeRegisterDto());

    show(wageTypeRegisterId?: number): void {
        this.active.set(true);

        if (!wageTypeRegisterId) {
            this.wageTypeRegister.set(new CreateOrEditWageTypeRegisterDto());
            this.modal().show();
        } else {
            this._wageTypeRegistersService.getWageTypeRegisterForEdit(wageTypeRegisterId).subscribe((result) => {
                this.wageTypeRegister.set(result.wageTypeRegister);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WageTypeRegister_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._wageTypeRegistersService
            .createOrEdit(this.wageTypeRegister())
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

    updateField(field: keyof CreateOrEditWageTypeRegisterDto, value: any): void {
        const current = this.wageTypeRegister();
        (current as any)[field] = value;
        const next = new CreateOrEditWageTypeRegisterDto();
        Object.assign(next, current);
        this.wageTypeRegister.set(next);
    }
}
