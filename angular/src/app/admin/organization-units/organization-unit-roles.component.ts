import { Component, effect, inject, input, output, viewChild } from '@angular/core';
import { AddRoleModalComponent } from '@app/admin/organization-units/add-role-modal.component';
import { AppComponentBase } from '@shared/common/app-component-base';
import { OrganizationUnitServiceProxy, OrganizationUnitRoleListDto } from '@shared/service-proxies/service-proxies';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { IBasicOrganizationUnitInfo } from './basic-organization-unit-info';
import { IRoleWithOrganizationUnit } from './role-with-organization-unit';
import { IRolesWithOrganizationUnit } from './roles-with-organization-unit';
import { finalize } from 'rxjs/operators';

import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AddRoleModalComponent as AddRoleModalComponent_1 } from './add-role-modal.component';
import { LuxonFormatPipe } from '../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

@Component({
    selector: 'organization-unit-roles',
    templateUrl: './organization-unit-roles.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        AddRoleModalComponent_1,
        LuxonFormatPipe,
        LocalizePipe,
        PermissionPipe,
    ],
})
export class OrganizationUnitRolesComponent extends AppComponentBase {
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);

    readonly organizationUnit = input<IBasicOrganizationUnitInfo>(null);

    readonly roleRemoved = output<IRoleWithOrganizationUnit>();
    readonly rolesAdded = output<IRolesWithOrganizationUnit>();

    readonly addRoleModal = viewChild<AddRoleModalComponent>('addRoleModal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    constructor() {
        super();
        effect(() => {
            const ou = this.organizationUnit();
            if (ou) {
                this.addRoleModal().organizationUnitId.set(ou.id);
                this.refreshRoles();
            }
        });
    }

    getOrganizationUnitRoles(event?: LazyLoadEvent) {
        if (!this.organizationUnit()) {
            return;
        }

        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);

            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();
        this._organizationUnitService
            .getOrganizationUnitRoles(
                this.organizationUnit().id,
                this.primengTableHelper.getSorting(this.dataTable()),
                this.primengTableHelper.getSkipCount(this.paginator(), event),
                this.primengTableHelper.getMaxResultCount(this.paginator(), event)
            )
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    reloadPage(): void {
        const rows = this.paginator().rows || this.primengTableHelper.defaultRecordsCountPerPage;
        this.getOrganizationUnitRoles({ first: this.paginator().getPage() * rows, rows } as LazyLoadEvent);
    }

    refreshRoles(): void {
        this.reloadPage();
    }

    openAddRoleModal(): void {
        this.addRoleModal().show();
    }

    removeRole(role: OrganizationUnitRoleListDto): void {
        this.message.confirm(
            this.l('RemoveRoleFromOuWarningMessage', role.displayName, this.organizationUnit().displayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._organizationUnitService
                        .removeRoleFromOrganizationUnit(role.id, this.organizationUnit().id)
                        .subscribe(() => {
                            this.notify.success(this.l('SuccessfullyRemoved'));
                            this.roleRemoved.emit({
                                roleId: role.id,
                                ouId: this.organizationUnit().id,
                            });

                            this.refreshRoles();
                        });
                }
            }
        );
    }

    addRoles(data: any): void {
        this.rolesAdded.emit({
            roleIds: data.roleIds,
            ouId: data.ouId,
        });

        this.refreshRoles();
    }
}
