import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPAEmployeeDto,
    EmployeeDto,
    EmployeesServiceProxy,
    FormTypeDto,
    FormTypesServiceProxy,
    GetAllEmployeesInput,
    GetAllFormTypesInput,
    GetAllPACyclesInput,
    PACycleDto,
    PACyclesServiceProxy,
    PAEmployeesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPAEmployeeModal',
    templateUrl: './create-or-edit-pa-employee-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAEmployeeModalComponent extends AppComponentBase {
    private _service = inject(PAEmployeesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _paCyclesService = inject(PACyclesServiceProxy);
    private _formTypesService = inject(FormTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly paEmployee = signal<CreateOrEditPAEmployeeDto>(new CreateOrEditPAEmployeeDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly paCycles = signal<PACycleDto[]>([]);
    readonly formTypes = signal<FormTypeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._paCyclesService
            .getAll(
                new GetAllPACyclesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.paCycles.set(result.items));

        this._formTypesService
            .getAll(
                new GetAllFormTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.formTypes.set(result.items));

        if (!id) {
            this.paEmployee.set(new CreateOrEditPAEmployeeDto());
            this.modal().show();
        } else {
            this._service.getPAEmployeeForEdit(id).subscribe((result) => {
                this.paEmployee.set(result.paEmployee);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAEmployee_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.paEmployee())
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

    updateField(field: keyof CreateOrEditPAEmployeeDto, value: any): void {
        const current = this.paEmployee();
        (current as any)[field] = value;
        const next = new CreateOrEditPAEmployeeDto();
        Object.assign(next, current);
        this.paEmployee.set(next);
    }
}
