import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    NotificationGroupsServiceProxy,
    CreateOrEditNotificationGroupDto,
    NotificationConfigDto,
    GetAllNotificationConfigsInput,
    NotificationConfigsServiceProxy,
    WorkflowRoleDto,
    WorkflowRolesServiceProxy,
    GetAllWorkflowRolesInput,
    WorkflowRoleGroup,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditNotificationGroupModal',
    templateUrl: './create-or-edit-notification-group-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditNotificationGroupModalComponent extends AppComponentBase {
    private _notificationGroupService = inject(NotificationGroupsServiceProxy);
    private _notificationConfigsService = inject(NotificationConfigsServiceProxy);
    private _workflowRolesService = inject(WorkflowRolesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly notificationGroup = signal<CreateOrEditNotificationGroupDto>(new CreateOrEditNotificationGroupDto());
    readonly notificationConfigs = signal<NotificationConfigDto[]>([]);
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);

    readonly workflowRoleGroups = [
        { value: WorkflowRoleGroup.None, key: 'WorkflowRoleGroupNone' },
        { value: WorkflowRoleGroup.Company, key: 'WorkflowRoleGroupCompany' },
        { value: WorkflowRoleGroup.Department, key: 'WorkflowRoleGroupDepartment' },
        { value: WorkflowRoleGroup.Branch, key: 'WorkflowRoleGroupBranch' },
        { value: WorkflowRoleGroup.CostCenter, key: 'WorkflowRoleGroupCostCenter' },
    ];

    show(notificationGroupId?: number): void {
        this.active.set(true);

        this._notificationConfigsService
            .getAll(
                new GetAllNotificationConfigsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.notificationConfigs.set(result.items);
            });

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

        if (!notificationGroupId) {
            this.notificationGroup.set(
                new CreateOrEditNotificationGroupDto({ workflowRoleGroup: WorkflowRoleGroup.None } as any)
            );
            this.modal().show();
        } else {
            this._notificationGroupService.getNotificationGroupForEdit(notificationGroupId).subscribe((result) => {
                this.notificationGroup.set(result.notificationGroup);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('NotificationGroup_NotificationConfigId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._notificationGroupService
            .createOrEdit(this.notificationGroup())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditNotificationGroupDto, value: any): void {
        const current = this.notificationGroup();
        (current as any)[field] = value;
        const next = new CreateOrEditNotificationGroupDto();
        Object.assign(next, current);
        this.notificationGroup.set(next);
    }
}
