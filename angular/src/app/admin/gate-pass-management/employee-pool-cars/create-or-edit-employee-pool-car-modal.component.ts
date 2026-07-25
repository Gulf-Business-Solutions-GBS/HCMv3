import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeePoolCarDto,
    EmployeeDto,
    EmployeePoolCarsServiceProxy,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllPoolCarsInput,
    PoolCarDto,
    PoolCarsServiceProxy,
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
    selector: 'createOrEditEmployeePoolCarModal',
    templateUrl: './create-or-edit-employee-pool-car-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeePoolCarModalComponent extends AppComponentBase {
    private _service = inject(EmployeePoolCarsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _poolCarsService = inject(PoolCarsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeePoolCarDto>(new CreateOrEditEmployeePoolCarDto());

    readonly employees = signal<EmployeeDto[]>([]);
    readonly poolCars = signal<PoolCarDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._poolCarsService
            .getAll(new GetAllPoolCarsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.poolCars.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditEmployeePoolCarDto());
            this.modal().show();
        } else {
            this._service.getEmployeePoolCarForEdit(id).subscribe((result) => {
                this.record.set(result.employeePoolCar);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeePoolCar_DriverEmployeeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeePoolCarDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeePoolCarDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'startDate' | 'endDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateTimeField(field: 'startTime' | 'endTime', dateField: 'startDate' | 'endDate', value: string): void {
        const current = this.record();
        if (!value) {
            (current as any)[field] = undefined;
        } else {
            const base = (current as any)[dateField] ?? DateTime.now();
            (current as any)[field] = DateTime.fromFormat(`${base.toFormat('yyyy-MM-dd')} ${value}`, 'yyyy-MM-dd HH:mm');
        }
        const next = new CreateOrEditEmployeePoolCarDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
