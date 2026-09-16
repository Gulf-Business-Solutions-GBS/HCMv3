import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditWageTypeDto, WageTypesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { WageTypeFormComponent } from './wage-type-form.component';

@Component({
    selector: 'createOrEditWageTypeModal',
    templateUrl: './create-or-edit-wage-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe, WageTypeFormComponent],
})
export class CreateOrEditWageTypeModalComponent extends AppComponentBase {
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly wageType = signal<CreateOrEditWageTypeDto>(new CreateOrEditWageTypeDto());

    show(wageTypeId?: number): void {
        this.active.set(true);

        if (!wageTypeId) {
            this.wageType.set(new CreateOrEditWageTypeDto());
            this.modal().show();
        } else {
            this._wageTypesService.getWageTypeForEdit(wageTypeId).subscribe((result) => {
                this.wageType.set(result.wageType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WageType_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._wageTypesService
            .createOrEdit(this.wageType())
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
