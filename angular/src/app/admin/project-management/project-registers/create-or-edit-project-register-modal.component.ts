import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditProjectRegisterDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    ProjectRegistersServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditProjectRegisterModal',
    templateUrl: './create-or-edit-project-register-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditProjectRegisterModalComponent extends AppComponentBase {
    private _service = inject(ProjectRegistersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditProjectRegisterDto>(new CreateOrEditProjectRegisterDto());
    readonly employees = signal<EmployeeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditProjectRegisterDto());
            this.modal().show();
        } else {
            this._service.getProjectRegisterForEdit(id).subscribe((result) => {
                this.record.set(result.projectRegister);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ProjectRegister_EmployeeId')?.focus();
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

    updateField(field: keyof CreateOrEditProjectRegisterDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditProjectRegisterDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditProjectRegisterDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditProjectRegisterDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
