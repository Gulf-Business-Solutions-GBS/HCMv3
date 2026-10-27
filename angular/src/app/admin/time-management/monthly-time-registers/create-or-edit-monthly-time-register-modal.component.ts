import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditMonthlyTimeRegisterDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    MonthlyTimeRegistersServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditMonthlyTimeRegisterModal',
    templateUrl: './create-or-edit-monthly-time-register-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditMonthlyTimeRegisterModalComponent extends AppComponentBase {
    private _monthlyTimeRegistersService = inject(MonthlyTimeRegistersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly monthlyTimeRegister = signal<CreateOrEditMonthlyTimeRegisterDto>(new CreateOrEditMonthlyTimeRegisterDto());
    readonly employees = signal<EmployeeDto[]>([]);

    show(monthlyTimeRegisterId?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        if (!monthlyTimeRegisterId) {
            this.monthlyTimeRegister.set(new CreateOrEditMonthlyTimeRegisterDto());
            this.modal().show();
        } else {
            this._monthlyTimeRegistersService
                .getMonthlyTimeRegisterForEdit(monthlyTimeRegisterId)
                .subscribe((result) => {
                    this.monthlyTimeRegister.set(result.monthlyTimeRegister);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('MonthlyTimeRegister_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._monthlyTimeRegistersService
            .createOrEdit(this.monthlyTimeRegister())
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

    updateField(field: keyof CreateOrEditMonthlyTimeRegisterDto, value: any): void {
        const current = this.monthlyTimeRegister();
        (current as any)[field] = value;
        const next = new CreateOrEditMonthlyTimeRegisterDto();
        Object.assign(next, current);
        this.monthlyTimeRegister.set(next);
    }

    updateDateField(field: keyof CreateOrEditMonthlyTimeRegisterDto, value: string): void {
        const current = this.monthlyTimeRegister();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditMonthlyTimeRegisterDto();
        Object.assign(next, current);
        this.monthlyTimeRegister.set(next);
    }

    updateMonthField(field: keyof CreateOrEditMonthlyTimeRegisterDto, value: string): void {
        const current = this.monthlyTimeRegister();
        (current as any)[field] = value ? DateTime.fromFormat(`${value}-01`, 'yyyy-MM-dd') : (undefined as any);
        const next = new CreateOrEditMonthlyTimeRegisterDto();
        Object.assign(next, current);
        this.monthlyTimeRegister.set(next);
    }
}
