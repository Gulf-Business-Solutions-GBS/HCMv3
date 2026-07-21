import { Component, inject, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EntityDtoOfInt64,
    UpdateUserPermissionsInput,
    UserServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { PermissionTreeComponent } from '../shared/permission-tree.component';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';

import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { Tooltip } from 'primeng/tooltip';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'editUserPermissionsModal',
    templateUrl: './edit-user-permissions-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, PermissionTreeComponent, ButtonBusyDirective, Tooltip, LocalizePipe],
})
export class EditUserPermissionsModalComponent extends AppComponentBase {
    private _userService = inject(UserServiceProxy);

    readonly modal = viewChild<ModalDirective>('editModal');
    readonly permissionTree = viewChild<PermissionTreeComponent>('permissionTree');

    readonly saving = signal<boolean>(false);
    readonly resettingPermissions = signal<boolean>(false);
    readonly userId = signal<number>(undefined);
    readonly userName = signal<string>(undefined);
    readonly permissionData = signal<any>(undefined);

    show(userId: number, userName?: string): void {
        this.userId.set(userId);
        this.userName.set(userName);

        this._userService.getUserPermissionsForEdit(userId).subscribe((result) => {
            this.permissionData.set(result);
            this.modal().show();
        });
    }

    save(): void {
        let input = new UpdateUserPermissionsInput();

        input.id = this.userId();
        input.grantedPermissionNames = this.permissionTree().getGrantedPermissionNames();

        this.saving.set(true);
        this._userService
            .updateUserPermissions(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
            });
    }

    resetPermissions(): void {
        let input = new EntityDtoOfInt64();

        input.id = this.userId();

        this.resettingPermissions.set(true);
        this._userService.resetUserSpecificPermissions(input).subscribe({
            next: () => {
                this.notify.info(this.l('ResetSuccessfully'));
                this._userService.getUserPermissionsForEdit(this.userId()).subscribe((result) => {
                    this.permissionData.set(result);
                });
            },
            complete: () => {
                this.resettingPermissions.set(false);
            },
        });
    }

    close(): void {
        this.modal().hide();
    }
}
