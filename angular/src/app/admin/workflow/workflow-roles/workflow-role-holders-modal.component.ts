import { Component, inject, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeWorkflowRoleDto,
    EmployeeWorkflowRolesServiceProxy,
    GetAllEmployeeWorkflowRolesInput,
    WorkflowRoleDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';

/**
 * Who holds one workflow role today -- the people a step pointing at the role would reach.
 *
 * <p>Answers "how many employees are assigned to this role, and who?" from the role list itself, instead of
 * opening every employee's record to look. Only assignments in force today are listed, because those are the
 * only ones approver routing can see.</p>
 */
@Component({
    selector: 'workflowRoleHoldersModal',
    templateUrl: './workflow-role-holders-modal.component.html',
    imports: [AppBsModalDirective, BusyIfDirective, LocalizePipe, LuxonFormatPipe],
})
export class WorkflowRoleHoldersModalComponent extends AppComponentBase {
    private _service = inject(EmployeeWorkflowRolesServiceProxy);

    readonly modal = viewChild<ModalDirective>('holdersModal');

    readonly roleName = signal<string>('');
    readonly holders = signal<EmployeeWorkflowRoleDto[]>([]);
    readonly loading = signal<boolean>(false);

    show(role: WorkflowRoleDto): void {
        this.roleName.set(role.nameEn);
        this.holders.set([]);
        this.loading.set(true);
        this.modal().show();

        this._service
            .getAll(
                new GetAllEmployeeWorkflowRolesInput({
                    workflowRoleId: role.id,
                    onlyActiveToday: true,
                    employeeId: undefined,
                    filter: undefined,
                    sorting: undefined,
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.holders.set(result.items));
    }

    close(): void {
        this.modal().hide();
    }
}
