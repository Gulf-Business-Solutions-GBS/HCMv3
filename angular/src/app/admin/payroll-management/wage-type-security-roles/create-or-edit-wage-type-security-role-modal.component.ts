import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWageTypeSecurityRoleDto,
    GetAllWageTypesInput,
    GetRolesInput,
    RoleListDto,
    RoleServiceProxy,
    WageTypeDto,
    WageTypeSecurityRolesServiceProxy,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditWageTypeSecurityRoleModal',
    templateUrl: './create-or-edit-wage-type-security-role-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWageTypeSecurityRoleModalComponent extends AppComponentBase {
    private _wageTypeSecurityRolesService = inject(WageTypeSecurityRolesServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _roleService = inject(RoleServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly wageTypeSecurityRole = signal<CreateOrEditWageTypeSecurityRoleDto>(new CreateOrEditWageTypeSecurityRoleDto());

    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly roles = signal<RoleListDto[]>([]);

    show(wageTypeSecurityRoleId?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        this._roleService.getRoles(new GetRolesInput({ permissions: undefined })).subscribe((result) => this.roles.set(result.items));

        if (!wageTypeSecurityRoleId) {
            this.wageTypeSecurityRole.set(new CreateOrEditWageTypeSecurityRoleDto());
            this.modal().show();
        } else {
            this._wageTypeSecurityRolesService.getWageTypeSecurityRoleForEdit(wageTypeSecurityRoleId).subscribe((result) => {
                this.wageTypeSecurityRole.set(result.wageTypeSecurityRole);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WageTypeSecurityRole_WageTypeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._wageTypeSecurityRolesService
            .createOrEdit(this.wageTypeSecurityRole())
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

    updateField(field: keyof CreateOrEditWageTypeSecurityRoleDto, value: any): void {
        const current = this.wageTypeSecurityRole();
        (current as any)[field] = value;
        const next = new CreateOrEditWageTypeSecurityRoleDto();
        Object.assign(next, current);
        this.wageTypeSecurityRole.set(next);
    }
}
