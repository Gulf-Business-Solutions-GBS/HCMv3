import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    NotificationConfigsServiceProxy,
    CreateOrEditNotificationConfigDto,
    EmailTemplateDto,
    EmailTemplatesServiceProxy,
    GetAllEmailTemplatesInput,
    GetAllOrganizationUnitTypesInput,
    GetAllWorkflowRolesInput,
    NotificationConfigGroupDto,
    OrganizationUnitTypeDto,
    OrganizationUnitTypesServiceProxy,
    WorkflowRoleDto,
    WorkflowRoleGroup,
    WorkflowRolesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditNotificationConfigModal',
    templateUrl: './create-or-edit-notification-config-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditNotificationConfigModalComponent extends AppComponentBase {
    private _notificationConfigService = inject(NotificationConfigsServiceProxy);
    private _emailTemplatesService = inject(EmailTemplatesServiceProxy);
    private _workflowRolesService = inject(WorkflowRolesServiceProxy);
    private _orgUnitTypesService = inject(OrganizationUnitTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly notificationConfig = signal<CreateOrEditNotificationConfigDto>(new CreateOrEditNotificationConfigDto());
    readonly emailTemplates = signal<EmailTemplateDto[]>([]);
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);
    readonly orgUnitTypes = signal<OrganizationUnitTypeDto[]>([]);

    readonly roleGroups = [
        { value: WorkflowRoleGroup.None, key: 'WorkflowRoleGroupNone' },
        { value: WorkflowRoleGroup.Company, key: 'WorkflowRoleGroupCompany' },
        { value: WorkflowRoleGroup.Department, key: 'WorkflowRoleGroupDepartment' },
        { value: WorkflowRoleGroup.Branch, key: 'WorkflowRoleGroupBranch' },
        { value: WorkflowRoleGroup.CostCenter, key: 'WorkflowRoleGroupCostCenter' },
    ];

    /**
     * Legacy rule: Send To All (every active employee) stands alone. With Employee, Request Creator, Manager or
     * a group also set, the same people would simply be told twice. The server refuses it too.
     */
    readonly sendToAllConflict = computed(() => {
        const c = this.notificationConfig();
        return (
            !!c.sendToAll && (!!c.toEmployee || !!c.toRequestCreator || !!c.toManager || (c.groups ?? []).length > 0)
        );
    });

    /** Every group line must name the role whose holders it tells. */
    readonly groupsComplete = computed(() => (this.notificationConfig().groups ?? []).every((g) => !!g.workflowRoleId));

    show(notificationConfigId?: number): void {
        this.active.set(true);

        this._emailTemplatesService
            .getAll(
                new GetAllEmailTemplatesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.emailTemplates.set(result.items));
        this._workflowRolesService
            .getAll(
                new GetAllWorkflowRolesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.workflowRoles.set(result.items));
        this._orgUnitTypesService
            .getAll(
                new GetAllOrganizationUnitTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.orgUnitTypes.set(result.items));

        if (!notificationConfigId) {
            this.notificationConfig.set(new CreateOrEditNotificationConfigDto({ groups: [] } as any));
            this.modal().show();
        } else {
            this._notificationConfigService.getNotificationConfigForEdit(notificationConfigId).subscribe((result) => {
                this.notificationConfig.set(result.notificationConfig);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('NotificationConfig_Name')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._notificationConfigService
            .createOrEdit(this.notificationConfig())
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

    addGroup(): void {
        const group = new NotificationConfigGroupDto();
        group.workflowRoleGroup = WorkflowRoleGroup.None;
        this.updateField('groups', [...(this.notificationConfig().groups ?? []), group]);
    }

    removeGroup(index: number): void {
        const groups = [...(this.notificationConfig().groups ?? [])];
        groups.splice(index, 1);
        this.updateField('groups', groups);
    }

    updateGroup(index: number, field: keyof NotificationConfigGroupDto, value: any): void {
        const groups = [...(this.notificationConfig().groups ?? [])];
        const group = new NotificationConfigGroupDto();
        Object.assign(group, groups[index]);
        (group as any)[field] = value ?? undefined;
        groups[index] = group;
        this.updateField('groups', groups);
    }

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditNotificationConfigDto, value: any): void {
        const current = this.notificationConfig();
        (current as any)[field] = value;
        const next = new CreateOrEditNotificationConfigDto();
        Object.assign(next, current);
        this.notificationConfig.set(next);
    }
}
