import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApprovalRequire,
    CreateOrEditRequestDetailDto,
    GetAllNotificationConfigsInput,
    GetAllOrgUnitsInput,
    GetAllRequestDetailsInput,
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

    /** The chain's name, shown read-only on the form so the step is unmistakably attached to it. */
    readonly requestMasterName = signal<string>('');

    /** True when another step in this chain is already the creator step. */
    readonly creatorStepTaken = signal<boolean>(false);

    /** True while the form holds a copy of the creator step, whose approver has been cleared. */
    readonly copiedCreatorStep = signal<boolean>(false);
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);
    readonly orgUnits = signal<OrgUnitDto[]>([]);
    readonly notificationConfigs = signal<NotificationConfigDto[]>([]);

    /**
     * Whether the seven per-action notification settings are open. Closed by default: almost every step uses
     * the standard ones (approver told on arrival and reminded; requester told of each decision), which the
     * server fills in for any left blank. Opened automatically for a step that already differs.
     */
    readonly showNotificationDetails = signal<boolean>(false);

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
     * The approver choices this step may take.
     *
     * <p>"None" -- the creator step -- is dropped once another step in the chain holds it. A second one is
     * not a second anything: approvals start at the first step naming somebody, so an extra creator step
     * sits mid-chain and is never asked to act.</p>
     */
    readonly availableApproverPersonTypes = computed(() =>
        this.creatorStepTaken() && !this.record().workflowRoleId
            ? this.approverPersonTypes.filter((t) => t.value !== WorkflowApproverPersonType.None)
            : this.approverPersonTypes
    );

    /**
     * True when this step names nobody although the chain already has its creator step.
     *
     * <p>A Workflow Role on its own is enough -- the step then goes to everyone holding the role -- so the
     * approver type is only demanded when no role is chosen either.</p>
     */
    readonly needsApprover = computed(() => {
        const record = this.record();
        const noApproverType =
            record.approverPerson === undefined ||
            record.approverPerson === null ||
            record.approverPerson === WorkflowApproverPersonType.None;

        return this.creatorStepTaken() && noApproverType && !record.workflowRoleId;
    });

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

    /**
     * @param requestMasterId the chain this step belongs to. Required: a step exists only inside a chain, and
     *                        the caller always knows which one because the grid is already scoped to it.
     * @param id the step to edit, or omitted to add one.
     */
    show(requestMasterId: number, id?: number): void {
        this.active.set(true);
        this.showNotificationDetails.set(false);
        this.copiedCreatorStep.set(false);

        this._requestMastersService
            .getAll(new GetAllRequestMastersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.requestMasters.set(result.items);
                this.requestMasterName.set(result.items.find((m) => m.id === requestMasterId)?.nameEn ?? '');
            });

        // Whether this chain already has its one creator step, so the dropdown can stop offering a second.
        // The server refuses one either way; this is so the choice is never presented rather than taken
        // and then rejected.
        this._service
            .getAll(
                new GetAllRequestDetailsInput({
                    requestMasterId,
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                })
            )
            .subscribe((result) => {
                this.creatorStepTaken.set(
                    result.items.some((d) => d.approverPerson === WorkflowApproverPersonType.None && d.id !== id)
                );
            });
        this._workflowRolesService
            .getAll(new GetAllWorkflowRolesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workflowRoles.set(result.items));
        this._orgUnitsService
            .getAll(new GetAllOrgUnitsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.orgUnits.set(result.items));
        this._notificationConfigsService
            .getAll(new GetAllNotificationConfigsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.notificationConfigs.set(result.items);
                this.openNotificationsIfCustomised();
            });

        if (!id) {
            this.record.set(
                new CreateOrEditRequestDetailDto({
                    requestMasterId,
                    workflowRoleGroup: WorkflowRoleGroup.None,
                    escalateWorkflowRoleGroup: WorkflowRoleGroup.None,
                    person: WorkflowPersonScope.None,
                    approverPerson: WorkflowApproverPersonType.None,
                    approvalRequire: ApprovalRequire.Single,
                    escalateAfterDays: 0,
                    reminderDays: 0,
                } as any)
            );
            this.modal().show();
        } else {
            this._service.getRequestDetailForEdit(id).subscribe((result) => {
                this.record.set(result.requestDetail);
                this.openNotificationsIfCustomised();
                this.modal().show();
            });
        }
    }

    /**
     * Opens a NEW step pre-filled from the creator step, with its approver cleared.
     *
     * <p>A chain has exactly one creator step, so a straight copy of it would be refused. Everything else
     * on it -- escalation, reminders, notifications, the switches -- is still worth carrying over, so the copy
     * is made here, unsaved, and the person is asked to choose who approves it before saving.</p>
     */
    showCopyOfCreatorStep(requestMasterId: number, sourceId: number): void {
        this.show(requestMasterId);
        this.copiedCreatorStep.set(true);

        this._service.getRequestDetailForEdit(sourceId).subscribe((result) => {
            const copy = new CreateOrEditRequestDetailDto();
            Object.assign(copy, result.requestDetail);
            copy.id = undefined;
            copy.approverPerson = undefined;
            copy.workflowRoleId = undefined;
            this.record.set(copy);
        });
    }

    /**
     * Opens the notification settings when this step uses anything but the standard ones, so a customised
     * step never hides its differences behind the one-line summary.
     */
    private openNotificationsIfCustomised(): void {
        const configs = this.notificationConfigs();
        if (configs.length === 0) {
            return;
        }

        const idOf = (name: string) => configs.find((c) => c.name === name)?.id;
        const toApprover = idOf('Workflow - Notify Approver');
        const toRequester = idOf('Workflow - Notify Requester');
        const approverFields: NotificationConfigField[] = [
            'submitNotificationConfigId',
            'assignNotificationConfigId',
            'reminderNotificationConfigId',
        ];

        const record = this.record();
        const customised = this.notificationActions.some(({ field }) => {
            const value = record[field];
            const standard = approverFields.includes(field) ? toApprover : toRequester;
            return value !== undefined && value !== null && value !== standard;
        });

        if (customised) {
            this.showNotificationDetails.set(true);
        }
    }

    onShown(): void {
        // The chain field is read-only now, so focus goes to the first thing worth typing into.
        document.getElementById('RequestDetail_Step')?.focus();
    }

    save(): void {
        // Escalate After Days and Reminder Days are optional: blank means "never", which the server
        // stores as 0.
        const record = new CreateOrEditRequestDetailDto();
        Object.assign(record, this.record());
        record.escalateAfterDays = record.escalateAfterDays ?? 0;
        record.reminderDays = record.reminderDays ?? 0;
        record.approverPerson = record.approverPerson ?? WorkflowApproverPersonType.None;

        this.saving.set(true);
        this._service
            .createOrEdit(record)
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
