import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApprovalRequire,
    CreateOrEditRequestDetailDto,
    GetAllNotificationConfigsInput,
    GetAllOrgUnitsInput,
    GetAllRequestMastersInput,
    GetAllWorkflowRolesInput,
    NotificationConfigDto,
    NotificationConfigsServiceProxy,
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

/**
 * The seven per-step notification settings, named so the template can index the DTO by one of them and still
 * type-check: the whole `keyof` would widen the binding to every property the DTO has.
 */
type NotificationConfigField =
    | 'notificationConfigId'
    | 'submitNotificationConfigId'
    | 'rejectNotificationConfigId'
    | 'sendBackNotificationConfigId'
    | 'remarksNotificationConfigId'
    | 'assignNotificationConfigId'
    | 'reminderNotificationConfigId';

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
    private _notificationConfigsService = inject(NotificationConfigsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditRequestDetailDto>(new CreateOrEditRequestDetailDto());
    readonly requestMasters = signal<RequestMasterDto[]>([]);
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);
    readonly orgUnits = signal<OrgUnitDto[]>([]);
    readonly notificationConfigs = signal<NotificationConfigDto[]>([]);

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

    /**
     * One notification setting per action, in the order a request meets them: it arrives, somebody acts on it,
     * and it is chased while nobody does. Listed here rather than repeated seven times in the template, which
     * is how one of them ends up bound to the wrong field.
     */
    /**
     * `domId` is spelled out rather than derived from `field`: F1 help finds a field by its element id, and a
     * camel-cased id would silently match nothing.
     */
    readonly notificationActions: { field: NotificationConfigField; domId: string; key: string }[] = [
        { field: 'submitNotificationConfigId', domId: 'RequestDetail_SubmitNotificationConfigId', key: 'OnSubmit' },
        { field: 'notificationConfigId', domId: 'RequestDetail_NotificationConfigId', key: 'OnApprove' },
        { field: 'rejectNotificationConfigId', domId: 'RequestDetail_RejectNotificationConfigId', key: 'OnReject' },
        { field: 'sendBackNotificationConfigId', domId: 'RequestDetail_SendBackNotificationConfigId', key: 'OnSendBack' },
        { field: 'remarksNotificationConfigId', domId: 'RequestDetail_RemarksNotificationConfigId', key: 'OnRemark' },
        { field: 'assignNotificationConfigId', domId: 'RequestDetail_AssignNotificationConfigId', key: 'OnReassign' },
        { field: 'reminderNotificationConfigId', domId: 'RequestDetail_ReminderNotificationConfigId', key: 'OnReminder' },
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
        this._notificationConfigsService
            .getAll(new GetAllNotificationConfigsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.notificationConfigs.set(result.items));

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
