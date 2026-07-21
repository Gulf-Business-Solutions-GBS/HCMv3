import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    FindOrganizationUnitRolesInput,
    NameValueDto,
    OrganizationUnitServiceProxy,
    RolesToOrganizationUnitInput,
} from '@shared/service-proxies/service-proxies';
import { map as _map } from 'lodash-es';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { LazyLoadEvent } from 'primeng/api';
import { Paginator, PaginatorModule } from 'primeng/paginator';
import { Table, TableModule } from 'primeng/table';
import { IRolesWithOrganizationUnit } from './roles-with-organization-unit';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../shared/utils/auto-focus.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';

import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'addRoleModal',
    templateUrl: './add-role-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        AutoFocusDirective,
        BusyIfDirective,
        TableModule,
        PaginatorModule,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class AddRoleModalComponent extends AppComponentBase {
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);

    readonly rolesAdded = output<IRolesWithOrganizationUnit>();
    readonly modal = viewChild<ModalDirective>('modal');
    readonly dataTable = viewChild<Table>('dataTable');
    readonly paginator = viewChild<Paginator>('paginator');

    readonly organizationUnitId = signal<number | undefined>(undefined);

    readonly isShown = signal<boolean>(false);
    readonly filterText = signal<string>('');
    readonly tenantId = signal<number | undefined>(undefined);
    readonly saving = signal<boolean>(false);
    readonly selectedRoles = signal<NameValueDto[]>([]);

    show(): void {
        this.modal().show();
    }

    refreshTable(): void {
        this.paginator().changePage(this.paginator().getPage());
        this.shown();
    }

    close(): void {
        this.modal().hide();
    }

    shown(): void {
        this.isShown.set(true);
        this.getRecordsIfNeeds(null);
    }

    getRecordsIfNeeds(event: LazyLoadEvent): void {
        if (!this.isShown()) {
            return;
        }

        this.getRecords(event);
    }

    getRecords(event?: LazyLoadEvent): void {
        if (this.primengTableHelper.shouldResetPaging(event)) {
            this.paginator().changePage(0);

            if (this.primengTableHelper.records() && this.primengTableHelper.records().length > 0) {
                return;
            }
        }

        this.primengTableHelper.showLoadingIndicator();

        const input = new FindOrganizationUnitRolesInput();
        input.organizationUnitId = this.organizationUnitId();
        input.filter = this.filterText();
        input.skipCount = this.primengTableHelper.getSkipCount(this.paginator(), event);
        input.maxResultCount = this.primengTableHelper.getMaxResultCount(this.paginator(), event);

        this._organizationUnitService
            .findRoles(input)
            .pipe(finalize(() => this.primengTableHelper.hideLoadingIndicator()))
            .subscribe((result) => {
                this.primengTableHelper.totalRecordsCount.set(result.totalCount);
                this.primengTableHelper.records.set(result.items);
                this.primengTableHelper.hideLoadingIndicator();
            });
    }

    addRolesToOrganizationUnit(): void {
        const input = new RolesToOrganizationUnitInput();
        input.organizationUnitId = this.organizationUnitId();
        input.roleIds = _map(this.selectedRoles(), (selectedRole) => Number(selectedRole.value));
        this.saving.set(true);
        this._organizationUnitService.addRolesToOrganizationUnit(input).subscribe(() => {
            this.notify.success(this.l('SuccessfullyAdded'));
            this.rolesAdded.emit({
                roleIds: input.roleIds,
                ouId: input.organizationUnitId,
            });
            this.saving.set(false);
            this.close();
            this.selectedRoles.set([]);
        });
    }
}
