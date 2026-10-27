import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrUpdateRoleInput, RoleEditDto, RoleServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { PermissionTreeComponent } from '../shared/permission-tree.component';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditRoleModal',
    templateUrl: './create-or-edit-role-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        TabsetComponent,
        TabDirective,
        ValidationMessagesComponent,
        PermissionTreeComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditRoleModalComponent extends AppComponentBase {
    private _roleService = inject(RoleServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly permissionTree = viewChild<PermissionTreeComponent>('permissionTree');

    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly role = signal<RoleEditDto>(new RoleEditDto());
    readonly permissionData = signal<any>(undefined);

    show(roleId?: number): void {
        this.active.set(true);

        this._roleService.getRoleForEdit(roleId).subscribe((result) => {
            this.role.set(result.role);
            this.permissionData.set(result);

            this.modal().show();
        });
    }

    onShown(): void {
        document.getElementById('RoleDisplayName').focus();
    }

    save(): void {
        const input = new CreateOrUpdateRoleInput();
        input.role = this.role();
        input.grantedPermissionNames = this.permissionTree().getGrantedPermissionNames();

        this.saving.set(true);
        this._roleService
            .createOrUpdateRole(input)
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

    updateRoleDisplayName(value: string): void {
        const current = this.role();
        current.displayName = value;
        this.notifyRoleChanged(current);
    }

    updateRoleIsDefault(value: boolean): void {
        const current = this.role();
        current.isDefault = value;
        this.notifyRoleChanged(current);
    }

    private notifyRoleChanged(current: RoleEditDto): void {
        const newRole = new RoleEditDto();
        Object.assign(newRole, current);
        this.role.set(newRole);
    }
}
