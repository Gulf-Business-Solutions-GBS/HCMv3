import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSysUserOrgUnitDataDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllOrganizationUnitTypesInput,
    GetRolesInput,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    OrganizationUnitTypeDto,
    OrganizationUnitTypesServiceProxy,
    RoleListDto,
    RoleServiceProxy,
    SysUserOrgUnitDataServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditSysUserOrgUnitDataModal',
    templateUrl: './create-or-edit-sys-user-org-unit-data-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSysUserOrgUnitDataModalComponent extends AppComponentBase {
    private _service = inject(SysUserOrgUnitDataServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _roleService = inject(RoleServiceProxy);
    private _organizationUnitTypesService = inject(OrganizationUnitTypesServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSysUserOrgUnitDataDto>(new CreateOrEditSysUserOrgUnitDataDto());

    readonly employees = signal<EmployeeDto[]>([]);
    readonly roles = signal<RoleListDto[]>([]);
    readonly orgLevels = signal<OrganizationUnitTypeDto[]>([]);
    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._roleService
            .getRoles(new GetRolesInput({ permissions: undefined }))
            .subscribe((result) => this.roles.set(result.items));

        this._organizationUnitTypesService
            .getAll(
                new GetAllOrganizationUnitTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.orgLevels.set(result.items));

        this._organizationUnitService.getAll().subscribe((result) => this.organizationUnits.set(result));

        if (!id) {
            const record = new CreateOrEditSysUserOrgUnitDataDto();
            record.showUnlocked = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getSysUserOrgUnitDataForEdit(id).subscribe((result) => {
                this.record.set(result.sysUserOrgUnitData);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SysUserOrgUnitData_EmployeeId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    updateField(field: keyof CreateOrEditSysUserOrgUnitDataDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSysUserOrgUnitDataDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
