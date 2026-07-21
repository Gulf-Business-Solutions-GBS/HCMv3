import { Component, ElementRef, inject, output, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrganizationUnitInput,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    UpdateOrganizationUnitInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

export interface IOrganizationUnitOnEdit {
    id?: number;
    parentId?: number;
    displayName?: string;
}

@Component({
    selector: 'createOrEditOrganizationUnitModal',
    templateUrl: './create-or-edit-unit-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        NgClass,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditUnitModalComponent extends AppComponentBase {
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly organizationUnitDisplayNameInput = viewChild<ElementRef>('organizationUnitDisplayName');

    readonly unitCreated = output<OrganizationUnitDto>();
    readonly unitUpdated = output<OrganizationUnitDto>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);

    readonly organizationUnit = signal<IOrganizationUnitOnEdit>({});

    onShown(): void {
        document.getElementById('OrganizationUnitDisplayName').focus();
    }

    show(organizationUnit: IOrganizationUnitOnEdit): void {
        this.organizationUnit.set(organizationUnit);
        this.active.set(true);
        this.modal().show();
    }

    save(): void {
        if (!this.organizationUnit().id) {
            this.createUnit();
        } else {
            this.updateUnit();
        }
    }

    createUnit() {
        const currentUnit = this.organizationUnit();
        const createInput = new CreateOrganizationUnitInput();
        createInput.parentId = currentUnit.parentId;
        createInput.displayName = currentUnit.displayName;

        this.saving.set(true);
        this._organizationUnitService
            .createOrganizationUnit(createInput)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result: OrganizationUnitDto) => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.unitCreated.emit(result);
            });
    }

    updateUnit() {
        const currentUnit = this.organizationUnit();
        const updateInput = new UpdateOrganizationUnitInput();
        updateInput.id = currentUnit.id;
        updateInput.displayName = currentUnit.displayName;

        this.saving.set(true);
        this._organizationUnitService
            .updateOrganizationUnit(updateInput)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result: OrganizationUnitDto) => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.unitUpdated.emit(result);
            });
    }

    close(): void {
        this.modal().hide();
        this.active.set(false);
    }

    updateDisplayName(displayName: string): void {
        const currentUnit = this.organizationUnit();
        this.organizationUnit.set({ ...currentUnit, displayName });
    }
}
