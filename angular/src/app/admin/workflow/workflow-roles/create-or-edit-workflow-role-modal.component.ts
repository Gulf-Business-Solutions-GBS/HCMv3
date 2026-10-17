import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWorkflowRoleDto,
    OrganizationUnitTypeDto,
    OrganizationUnitTypesServiceProxy,
    GetAllOrganizationUnitTypesInput,
    WorkflowRoleGroup,
    WorkflowRolesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditWorkflowRoleModal',
    templateUrl: './create-or-edit-workflow-role-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWorkflowRoleModalComponent extends AppComponentBase {
    private _service = inject(WorkflowRolesServiceProxy);
    private _orgLevelsService = inject(OrganizationUnitTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditWorkflowRoleDto>(new CreateOrEditWorkflowRoleDto());
    readonly orgLevels = signal<OrganizationUnitTypeDto[]>([]);

    readonly workflowRoleGroups = [
        { value: WorkflowRoleGroup.None, key: 'WorkflowRoleGroupNone' },
        { value: WorkflowRoleGroup.Company, key: 'WorkflowRoleGroupCompany' },
        { value: WorkflowRoleGroup.Department, key: 'WorkflowRoleGroupDepartment' },
        { value: WorkflowRoleGroup.Branch, key: 'WorkflowRoleGroupBranch' },
        { value: WorkflowRoleGroup.CostCenter, key: 'WorkflowRoleGroupCostCenter' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._orgLevelsService
            .getAll(new GetAllOrganizationUnitTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.orgLevels.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditWorkflowRoleDto({ workflowRoleGroup: WorkflowRoleGroup.None } as any));
            this.modal().show();
        } else {
            this._service.getWorkflowRoleForEdit(id).subscribe((result) => {
                this.record.set(result.workflowRole);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WorkflowRole_Code')?.focus();
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

    updateField(field: keyof CreateOrEditWorkflowRoleDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditWorkflowRoleDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
