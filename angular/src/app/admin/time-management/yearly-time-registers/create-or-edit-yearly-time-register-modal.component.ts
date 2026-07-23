import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditYearlyTimeRegisterDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    YearlyTimeRegistersServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditYearlyTimeRegisterModal',
    templateUrl: './create-or-edit-yearly-time-register-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditYearlyTimeRegisterModalComponent extends AppComponentBase {
    private _yearlyTimeRegistersService = inject(YearlyTimeRegistersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly yearlyTimeRegister = signal<CreateOrEditYearlyTimeRegisterDto>(new CreateOrEditYearlyTimeRegisterDto());
    readonly employees = signal<EmployeeDto[]>([]);

    show(yearlyTimeRegisterId?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        if (!yearlyTimeRegisterId) {
            this.yearlyTimeRegister.set(new CreateOrEditYearlyTimeRegisterDto());
            this.modal().show();
        } else {
            this._yearlyTimeRegistersService.getYearlyTimeRegisterForEdit(yearlyTimeRegisterId).subscribe((result) => {
                this.yearlyTimeRegister.set(result.yearlyTimeRegister);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('YearlyTimeRegister_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._yearlyTimeRegistersService
            .createOrEdit(this.yearlyTimeRegister())
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

    updateField(field: keyof CreateOrEditYearlyTimeRegisterDto, value: any): void {
        const current = this.yearlyTimeRegister();
        (current as any)[field] = value;
        const next = new CreateOrEditYearlyTimeRegisterDto();
        Object.assign(next, current);
        this.yearlyTimeRegister.set(next);
    }

    updateDateField(field: keyof CreateOrEditYearlyTimeRegisterDto, value: string): void {
        const current = this.yearlyTimeRegister();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditYearlyTimeRegisterDto();
        Object.assign(next, current);
        this.yearlyTimeRegister.set(next);
    }

    updateYearField(field: keyof CreateOrEditYearlyTimeRegisterDto, value: string): void {
        const current = this.yearlyTimeRegister();
        (current as any)[field] = value ? DateTime.fromFormat(`${value}-01-01`, 'yyyy-MM-dd') : (undefined as any);
        const next = new CreateOrEditYearlyTimeRegisterDto();
        Object.assign(next, current);
        this.yearlyTimeRegister.set(next);
    }
}
