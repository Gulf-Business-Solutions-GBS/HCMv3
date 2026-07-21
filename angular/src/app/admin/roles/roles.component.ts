import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { RoleListDto, RoleServiceProxy, GetRolesInput } from '@shared/service-proxies/service-proxies';
import { Table, TableModule } from 'primeng/table';
import { CreateOrEditRoleModalComponent } from './create-or-edit-role-modal.component';
import { filter as _filter } from 'lodash-es';
import { finalize } from 'rxjs/operators';
import { PermissionTreeModalComponent } from '../shared/permission-tree-modal.component';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';

import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { Tooltip } from 'primeng/tooltip';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { TieredMenu } from 'primeng/tieredmenu';
import { MenuItem } from 'primeng/api';

@Component({
    templateUrl: './roles.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        PermissionTreeModalComponent,
        BusyIfDirective,
        TableModule,
        TieredMenu,
        Tooltip,
        CreateOrEditRoleModalComponent,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
    ],
})
export class RolesComponent extends AppComponentBase implements OnInit {
    private _roleService = inject(RoleServiceProxy);
    private _router = inject(Router);

    readonly createOrEditRoleModal = viewChild<CreateOrEditRoleModalComponent>('createOrEditRoleModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly permissionFilterTreeModal = viewChild<PermissionTreeModalComponent>('permissionFilterTreeModal');

    _entityTypeFullName = 'HCMv3.Authorization.Roles.Role';
    readonly entityHistoryEnabled = signal<boolean>(false);
    readonly activeRoleMenuItems = signal<MenuItem[]>([]);

    showRoleMenu(event: Event, role: RoleListDto, menu: any): void {
        this.activeRoleMenuItems.set(this.getMenuItemsForRole(role));
        menu.toggle(event);
    }

    getMenuItemsForRole(role: RoleListDto): MenuItem[] {
        const items: MenuItem[] = [];

        if (this.permission.isGranted('Pages.Administration.Roles.Edit')) {
            items.push({
                label: this.l('Edit'),
                icon: 'pi pi-pencil',
                command: () => this.createOrEditRoleModal().show(role.id),
            });
        }

        if (!role.isStatic && this.permission.isGranted('Pages.Administration.Roles.Delete')) {
            items.push({
                label: this.l('Delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteRole(role),
            });
        }

        if (this.entityHistoryEnabled()) {
            items.push({
                label: this.l('History'),
                icon: 'pi pi-history',
                command: () => this.showHistory(role),
            });
        }

        return items;
    }

    ngOnInit(): void {
        this.setIsEntityHistoryEnabled();
    }

    getRoles(): void {
        this.primengTableHelper.showLoadingIndicator();
        let selectedPermissions = this.permissionFilterTreeModal().getSelectedPermissions();

        this._roleService
            .getRoles(new GetRolesInput({ permissions: selectedPermissions }))
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.totalRecordsCount.set(result.items.length);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    createRole(): void {
        this.createOrEditRoleModal().show();
    }

    showHistory(role: RoleListDto): void {
        this._router.navigate([abp.appPath + `app/admin/entity-changes/${role.id}/${this._entityTypeFullName}`]);
    }

    deleteRole(role: RoleListDto): void {
        let self = this;
        self.message.confirm(
            self.l('RoleDeleteWarningMessage', role.displayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._roleService.deleteRole(role.id).subscribe(() => {
                        this.getRoles();
                        abp.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            }
        );
    }

    private setIsEntityHistoryEnabled(): void {
        let customSettings = (abp as any).custom;
        this.entityHistoryEnabled.set(
            customSettings.EntityHistory &&
                customSettings.EntityHistory.isEnabled &&
                _filter(
                    customSettings.EntityHistory.enabledEntities,
                    (entityType) => entityType === this._entityTypeFullName
                ).length === 1
        );
    }
}
