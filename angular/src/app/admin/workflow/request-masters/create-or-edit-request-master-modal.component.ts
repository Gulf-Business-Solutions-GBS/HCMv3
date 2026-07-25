import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRequestMasterDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    RequestMastersServiceProxy,
    WorkflowSecurityLevel,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditRequestMasterModal',
    templateUrl: './create-or-edit-request-master-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditRequestMasterModalComponent extends AppComponentBase {
    private _service = inject(RequestMastersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditRequestMasterDto>(new CreateOrEditRequestMasterDto());
    readonly employees = signal<EmployeeDto[]>([]);

    readonly securityLevels = [
        { value: WorkflowSecurityLevel.EmployeeOnly, key: 'WorkflowSecurityLevelEmployeeOnly' },
        { value: WorkflowSecurityLevel.EmployeeManager, key: 'WorkflowSecurityLevelEmployeeManager' },
        { value: WorkflowSecurityLevel.EmployeeDepartment, key: 'WorkflowSecurityLevelEmployeeDepartment' },
        { value: WorkflowSecurityLevel.Everyone, key: 'WorkflowSecurityLevelEveryone' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditRequestMasterDto({ securityLevel: WorkflowSecurityLevel.EmployeeOnly } as any));
            this.modal().show();
        } else {
            this._service.getRequestMasterForEdit(id).subscribe((result) => {
                this.record.set(result.requestMaster);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('RequestMaster_Code')?.focus();
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

    updateField(field: keyof CreateOrEditRequestMasterDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditRequestMasterDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
