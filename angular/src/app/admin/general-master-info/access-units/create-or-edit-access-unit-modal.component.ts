import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccessUnitsServiceProxy, CreateOrEditAccessUnitDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditAccessUnitModal',
    templateUrl: './create-or-edit-access-unit-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditAccessUnitModalComponent extends AppComponentBase {
    private _accessUnitsService = inject(AccessUnitsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly accessUnit = signal<CreateOrEditAccessUnitDto>(new CreateOrEditAccessUnitDto());

    show(accessUnitId?: number): void {
        this.active.set(true);

        if (!accessUnitId) {
            this.accessUnit.set(new CreateOrEditAccessUnitDto());
            this.modal().show();
        } else {
            this._accessUnitsService.getAccessUnitForEdit(accessUnitId).subscribe((result) => {
                this.accessUnit.set(result.accessUnit);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AccessUnit_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._accessUnitsService
            .createOrEdit(this.accessUnit())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditAccessUnitDto, value: any): void {
        const current = this.accessUnit();
        (current as any)[field] = value;
        const next = new CreateOrEditAccessUnitDto();
        Object.assign(next, current);
        this.accessUnit.set(next);
    }
}
