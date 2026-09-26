import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeWorkflowRoleDto,
    EmployeeDto,
    EmployeesServiceProxy,
    EmployeeWorkflowRolesServiceProxy,
    GetAllEmployeesInput,
    GetAllWorkflowRolesInput,
    WorkflowRoleDto,
    WorkflowRolesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * Assigns one employee to one workflow role for a date range.
 *
 * <p>The dates are the whole point and the easiest thing to get wrong: approver resolution only sees an
 * assignment while today falls inside the range, so a row dated in the past is a chain that reaches nobody.
 * A new assignment therefore starts today by default rather than empty.</p>
 */
@Component({
    selector: 'createOrEditEmployeeWorkflowRoleModal',
    templateUrl: './create-or-edit-employee-workflow-role-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeWorkflowRoleModalComponent extends AppComponentBase {
    private _service = inject(EmployeeWorkflowRolesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _workflowRolesService = inject(WorkflowRolesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeWorkflowRoleDto>(new CreateOrEditEmployeeWorkflowRoleDto());
    readonly employees = signal<EmployeeDto[]>([]);

    /** The employee's name, shown read-only: the section already belongs to one person. */
    readonly employeeName = signal<string>('');
    readonly workflowRoles = signal<WorkflowRoleDto[]>([]);

    /**
     * True when the range being entered does not include today, so the assignment will save correctly and
     * still resolve to nobody. Shown rather than blocked: dating an assignment ahead of a transfer is a
     * legitimate thing to do, and so is recording one that has ended.
     */
    readonly isNotActiveToday = signal<boolean>(false);

    /**
     * @param employeeId whose record this is. Always known, because the section lives inside one employee.
     * @param id the assignment to edit, or omitted to add one.
     */
    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.employees.set(result.items);

                const employee = result.items.find((e) => e.id === employeeId);
                this.employeeName.set(employee ? this.employeeLabel(employee) : '');
            });

        this._workflowRolesService
            .getAll(new GetAllWorkflowRolesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workflowRoles.set(result.items));

        if (!id) {
            // Starts today and runs to the end of 2099 rather than opening empty. An assignment is almost
            // always "from now until somebody says otherwise", and an open-ended default is what makes the
            // common case one field instead of three.
            const record = new CreateOrEditEmployeeWorkflowRoleDto();
            record.employeeId = employeeId;
            record.startDate = DateTime.now().startOf('day');
            record.endDate = DateTime.fromISO('2099-12-31');
            this.record.set(record);
            this.refreshWarning();
            this.modal().show();
        } else {
            this._service.getEmployeeWorkflowRoleForEdit(id).subscribe((result) => {
                this.record.set(result.employeeWorkflowRole);
                this.refreshWarning();
                this.modal().show();
            });
        }
    }

    onShown(): void {
        // The employee field is read-only here, so focus goes to the first thing worth choosing.
        document.getElementById('EmployeeWorkflowRole_WorkflowRoleId')?.focus();
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditEmployeeWorkflowRoleDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;

        // A new instance, not a mutation: the signal compares by reference, so editing in place leaves the
        // template and the warning below reading stale values.
        const next = new CreateOrEditEmployeeWorkflowRoleDto();
        Object.assign(next, current);
        this.record.set(next);

        this.refreshWarning();
    }

    updateDateField(field: keyof CreateOrEditEmployeeWorkflowRoleDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    private refreshWarning(): void {
        const record = this.record();

        if (!record.startDate || !record.endDate) {
            this.isNotActiveToday.set(false);
            return;
        }

        const today = DateTime.now().startOf('day');

        this.isNotActiveToday.set(
            record.startDate.startOf('day') > today || record.endDate.startOf('day') < today
        );
    }
}
