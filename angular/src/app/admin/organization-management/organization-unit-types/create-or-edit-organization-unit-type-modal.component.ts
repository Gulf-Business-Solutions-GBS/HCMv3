import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditOrganizationUnitTypeDto,
    OrganizationUnitTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditOrganizationUnitTypeModal',
    templateUrl: './create-or-edit-organization-unit-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditOrganizationUnitTypeModalComponent extends AppComponentBase {
    private _organizationUnitTypesService = inject(OrganizationUnitTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly organizationUnitType = signal<CreateOrEditOrganizationUnitTypeDto>(
        new CreateOrEditOrganizationUnitTypeDto()
    );

    show(organizationUnitTypeId?: number): void {
        this.active.set(true);

        if (!organizationUnitTypeId) {
            this.organizationUnitType.set(new CreateOrEditOrganizationUnitTypeDto());
            this.modal().show();
        } else {
            this._organizationUnitTypesService
                .getOrganizationUnitTypeForEdit(organizationUnitTypeId)
                .subscribe((result) => {
                    this.organizationUnitType.set(result.organizationUnitType);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('OrganizationUnitType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._organizationUnitTypesService
            .createOrEdit(this.organizationUnitType())
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

    updateField(field: keyof CreateOrEditOrganizationUnitTypeDto, value: any): void {
        const current = this.organizationUnitType();
        (current as any)[field] = value;
        const next = new CreateOrEditOrganizationUnitTypeDto();
        Object.assign(next, current);
        this.organizationUnitType.set(next);
    }
}
