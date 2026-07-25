import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApprovalRequire,
    CreateOrEditRequestDetailDto,
    GetAllOrgUnitsInput,
    GetAllRequestMastersInput,
    GetAllWorkflowRolesInput,
    OrgUnitDto,
    OrgUnitsServiceProxy,
    RequestDetailsServiceProxy,
    RequestMasterDto,
    RequestMastersServiceProxy,
    WorkflowApproverPersonType,
    WorkflowPersonScope,
    WorkflowRoleDto,
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
    selector: 'createOrEditRequestDetailModal',
    templateUrl: './create-or-edit-request-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditRequestDetailModalComponent extends AppComponentBase {
    private _service = inject(RequestDetailsServiceProxy);
    private _requestMastersService = inject(RequestMastersServiceProxy);
    private _workflowRolesService = inject(WorkflowRolesServiceProxy);
    private _orgUnitsService = inject(OrgUnitsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditRequestDetailDto>(new CreateOrEditRequestDetailDto());
    readonly requestMasters = signal<RequestMasterDto[]>([]);
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);
    readonly orgUnits = signal<OrgUnitDto[]>([]);

    readonly workflowRoleGroups = [
        { value: WorkflowRoleGroup.None, key: 'WorkflowRoleGroupNone' },
        { value: WorkflowRoleGroup.Company, key: 'WorkflowRoleGroupCompany' },
        { value: WorkflowRoleGroup.Department, key: 'WorkflowRoleGroupDepartment' },
        { value: WorkflowRoleGroup.Branch, key: 'WorkflowRoleGroupBranch' },
        { value: WorkflowRoleGroup.CostCenter, key: 'WorkflowRoleGroupCostCenter' },
    ];

    readonly personScopes = [
        { value: WorkflowPersonScope.None, key: 'WorkflowPersonScopeNone' },
        { value: WorkflowPersonScope.Themselves, key: 'WorkflowPersonScopeThemselves' },
        { value: WorkflowPersonScope.Department, key: 'WorkflowPersonScopeDepartment' },
        { value: WorkflowPersonScope.SelfSubordinates, key: 'WorkflowPersonScopeSelfSubordinates' },
        { value: WorkflowPersonScope.All, key: 'WorkflowPersonScopeAll' },
    ];

    readonly approverPersonTypes = [
        { value: WorkflowApproverPersonType.None, key: 'WorkflowApproverPersonTypeNone' },
        { value: WorkflowApproverPersonType.Person, key: 'WorkflowApproverPersonTypePerson' },
        { value: WorkflowApproverPersonType.PersonManager, key: 'WorkflowApproverPersonTypePersonManager' },
        { value: WorkflowApproverPersonType.ManagerLevel2, key: 'WorkflowApproverPersonTypeManagerLevel2' },
        { value: WorkflowApproverPersonType.ManagerLevel3, key: 'WorkflowApproverPersonTypeManagerLevel3' },
        { value: WorkflowApproverPersonType.ManagerLevel4, key: 'WorkflowApproverPersonTypeManagerLevel4' },
        { value: WorkflowApproverPersonType.ManagerLevel5, key: 'WorkflowApproverPersonTypeManagerLevel5' },
        { value: WorkflowApproverPersonType.ManagerLevel6, key: 'WorkflowApproverPersonTypeManagerLevel6' },
        { value: WorkflowApproverPersonType.ManagerLevel7, key: 'WorkflowApproverPersonTypeManagerLevel7' },
    ];

    readonly approvalRequires = [
        { value: ApprovalRequire.Single, key: 'ApprovalRequireSingle' },
        { value: ApprovalRequire.All, key: 'ApprovalRequireAll' },
        { value: ApprovalRequire.Parallel, key: 'ApprovalRequireParallel' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._requestMastersService
            .getAll(new GetAllRequestMastersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.requestMasters.set(result.items));
        this._workflowRolesService
            .getAll(new GetAllWorkflowRolesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workflowRoles.set(result.items));
        this._orgUnitsService
            .getAll(new GetAllOrgUnitsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.orgUnits.set(result.items));

        if (!id) {
            this.record.set(
                new CreateOrEditRequestDetailDto({
                    workflowRoleGroup: WorkflowRoleGroup.None,
                    escalateWorkflowRoleGroup: WorkflowRoleGroup.None,
                    person: WorkflowPersonScope.None,
                    approverPerson: WorkflowApproverPersonType.None,
                    approvalRequire: ApprovalRequire.Single,
                } as any)
            );
            this.modal().show();
        } else {
            this._service.getRequestDetailForEdit(id).subscribe((result) => {
                this.record.set(result.requestDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RequestDetail_RequestMasterId')?.focus();
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

    updateField(field: keyof CreateOrEditRequestDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditRequestDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
