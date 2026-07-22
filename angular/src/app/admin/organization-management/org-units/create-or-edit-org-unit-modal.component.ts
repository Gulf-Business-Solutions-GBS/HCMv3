import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditOrgUnitDto,
    GetAllSimpleLookupsInput,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    OrganizationUnitTypesServiceProxy,
    OrgUnitsServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditOrgUnitModal',
    templateUrl: './create-or-edit-org-unit-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditOrgUnitModalComponent extends AppComponentBase {
    private _orgUnitsService = inject(OrgUnitsServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _organizationUnitTypesService = inject(OrganizationUnitTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly orgUnit = signal<CreateOrEditOrgUnitDto>(new CreateOrEditOrgUnitDto());
    readonly allOrganizationUnits = signal<OrganizationUnitDto[]>([]);
    readonly organizationUnitTypes = signal<SimpleLookupDto[]>([]);

    /** Parent dropdown options: excludes the record's own id so a unit can't be made its own parent. */
    readonly parentOptions = computed(() => {
        const ownId = this.orgUnit()?.id;
        return this.allOrganizationUnits().filter((unit) => unit.id !== ownId);
    });

    show(orgUnitId?: number): void {
        this.active.set(true);

        this._organizationUnitService.getAll().subscribe((result) => {
            this.allOrganizationUnits.set(result);
        });

        this._organizationUnitTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.organizationUnitTypes.set(result.items);
            });

        if (!orgUnitId) {
            this.orgUnit.set(new CreateOrEditOrgUnitDto());
            this.modal().show();
        } else {
            this._orgUnitsService.getOrgUnitForEdit(orgUnitId).subscribe((result) => {
                this.orgUnit.set(result.orgUnit);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('OrgUnit_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._orgUnitsService
            .createOrEdit(this.orgUnit())
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

    updateField(field: keyof CreateOrEditOrgUnitDto, value: any): void {
        const current = this.orgUnit();
        (current as any)[field] = value;
        const next = new CreateOrEditOrgUnitDto();
        Object.assign(next, current);
        this.orgUnit.set(next);
    }
}
