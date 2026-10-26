import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSimulationTimeRegisterDto,
    DutyPeriodDto,
    DutyPeriodsServiceProxy,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllDutyPeriodsInput,
    GetAllEmployeesInput,
    SimulationTimeRegistersServiceProxy,
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
    selector: 'createOrEditSimulationTimeRegisterModal',
    templateUrl: './create-or-edit-simulation-time-register-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditSimulationTimeRegisterModalComponent extends AppComponentBase {
    private _simulationTimeRegistersService = inject(SimulationTimeRegistersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _dutyPeriodsService = inject(DutyPeriodsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly simulationTimeRegister = signal<CreateOrEditSimulationTimeRegisterDto>(
        new CreateOrEditSimulationTimeRegisterDto()
    );
    readonly employees = signal<EmployeeDto[]>([]);
    readonly dutyPeriods = signal<DutyPeriodDto[]>([]);

    show(simulationTimeRegisterId?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._dutyPeriodsService
            .getAll(
                new GetAllDutyPeriodsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.dutyPeriods.set(result.items));

        if (!simulationTimeRegisterId) {
            this.simulationTimeRegister.set(new CreateOrEditSimulationTimeRegisterDto());
            this.modal().show();
        } else {
            this._simulationTimeRegistersService
                .getSimulationTimeRegisterForEdit(simulationTimeRegisterId)
                .subscribe((result) => {
                    this.simulationTimeRegister.set(result.simulationTimeRegister);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('SimulationTimeRegister_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._simulationTimeRegistersService
            .createOrEdit(this.simulationTimeRegister())
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

    updateField(field: keyof CreateOrEditSimulationTimeRegisterDto, value: any): void {
        const current = this.simulationTimeRegister();
        (current as any)[field] = value;
        const next = new CreateOrEditSimulationTimeRegisterDto();
        Object.assign(next, current);
        this.simulationTimeRegister.set(next);
    }

    updateDateField(field: keyof CreateOrEditSimulationTimeRegisterDto, value: string): void {
        const current = this.simulationTimeRegister();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditSimulationTimeRegisterDto();
        Object.assign(next, current);
        this.simulationTimeRegister.set(next);
    }
}
