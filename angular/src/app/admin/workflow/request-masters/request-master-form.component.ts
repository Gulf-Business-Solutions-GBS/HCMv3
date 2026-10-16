import { Component, OnInit, inject, input, model, signal } from '@angular/core';
import { ControlContainer, FormsModule, NgForm } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRequestMasterDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    WorkflowSecurityLevel,
} from '@shared/service-proxies/service-proxies';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * The chain's own fields, shared by the Create modal and the chain detail page.
 *
 * It holds no form of its own: the controls register with the host's `<form #...="ngForm">` through the view
 * provider below, so the host's Save button sees their validity exactly as it did when the fields lived
 * directly in the modal.
 */
@Component({
    selector: 'requestMasterForm',
    templateUrl: './request-master-form.component.html',
    viewProviders: [{ provide: ControlContainer, useExisting: NgForm }],
    imports: [FormsModule, ValidationMessagesComponent, LocalizePipe],
})
export class RequestMasterFormComponent extends AppComponentBase implements OnInit {
    private _employeesService = inject(EmployeesServiceProxy);

    readonly requestMaster = model.required<CreateOrEditRequestMasterDto>();
    readonly readOnly = input<boolean>(false);

    readonly employees = signal<EmployeeDto[]>([]);

    readonly securityLevels = [
        { value: WorkflowSecurityLevel.EmployeeOnly, key: 'WorkflowSecurityLevelEmployeeOnly' },
        { value: WorkflowSecurityLevel.EmployeeManager, key: 'WorkflowSecurityLevelEmployeeManager' },
        { value: WorkflowSecurityLevel.EmployeeDepartment, key: 'WorkflowSecurityLevelEmployeeDepartment' },
        { value: WorkflowSecurityLevel.Everyone, key: 'WorkflowSecurityLevelEveryone' },
    ];

    ngOnInit(): void {
        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditRequestMasterDto, value: any): void {
        // A new instance, not a mutation: the model signal compares by reference, so editing in place leaves
        // the host reading stale values.
        const next = new CreateOrEditRequestMasterDto();
        Object.assign(next, this.requestMaster());
        // Auto Close In Days and Due In Days are optional: cleared means 0, which the server reads as "not set".
        const optionalDays = field === 'autoCloseInDays' || field === 'dueInDays';
        (next as any)[field] = optionalDays && (value === null || value === undefined || value === '') ? 0 : value;
        this.requestMaster.set(next);
    }
}
