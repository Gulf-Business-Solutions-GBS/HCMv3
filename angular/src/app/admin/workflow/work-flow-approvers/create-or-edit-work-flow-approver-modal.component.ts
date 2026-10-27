import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditWorkFlowApproverDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllWorkflowRequestsInput,
    WorkFlowApproversServiceProxy,
    WorkflowRequestDto,
    WorkflowRequestsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditWorkFlowApproverModal',
    templateUrl: './create-or-edit-work-flow-approver-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWorkFlowApproverModalComponent extends AppComponentBase {
    private _service = inject(WorkFlowApproversServiceProxy);
    private _workflowRequestsService = inject(WorkflowRequestsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditWorkFlowApproverDto>(new CreateOrEditWorkFlowApproverDto());
    readonly workflowRequests = signal<WorkflowRequestDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._workflowRequestsService
            .getAll(
                new GetAllWorkflowRequestsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    employeeId: undefined,
                })
            )
            .subscribe((result) => this.workflowRequests.set(result.items));
        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditWorkFlowApproverDto());
            this.modal().show();
        } else {
            this._service.getWorkFlowApproverForEdit(id).subscribe((result) => {
                this.record.set(result.workFlowApprover);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WorkFlowApprover_WorkflowRequestId')?.focus();
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

    updateField(field: keyof CreateOrEditWorkFlowApproverDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditWorkFlowApproverDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
