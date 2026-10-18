import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTimeRegisterDto,
    DutiesServiceProxy,
    DutyDto,
    DutyPeriodDto,
    DutyPeriodsServiceProxy,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllDutiesInput,
    GetAllDutyPeriodsInput,
    GetAllEmployeesInput,
    GetAllOvertimeTypesInput,
    GetAllTimePoliciesInput,
    OvertimeTypeDto,
    OvertimeTypesServiceProxy,
    TimePolicyDto,
    TimePoliciesServiceProxy,
    TimeRegisterStatus,
    TimeRegistersServiceProxy,
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
    selector: 'createOrEditTimeRegisterModal',
    templateUrl: './create-or-edit-time-register-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTimeRegisterModalComponent extends AppComponentBase {
    private _timeRegistersService = inject(TimeRegistersServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _dutyPeriodsService = inject(DutyPeriodsServiceProxy);
    private _dutiesService = inject(DutiesServiceProxy);
    private _timePoliciesService = inject(TimePoliciesServiceProxy);
    private _overtimeTypesService = inject(OvertimeTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly timeRegister = signal<CreateOrEditTimeRegisterDto>(new CreateOrEditTimeRegisterDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly dutyPeriods = signal<DutyPeriodDto[]>([]);
    readonly duties = signal<DutyDto[]>([]);
    readonly timePolicies = signal<TimePolicyDto[]>([]);
    readonly overtimeTypes = signal<OvertimeTypeDto[]>([]);

    readonly statuses = [
        { value: TimeRegisterStatus.Opened, name: 'Opened' },
        { value: TimeRegisterStatus.Approved, name: 'Approved' },
        { value: TimeRegisterStatus.Reopened, name: 'Reopened' },
    ];

    show(timeRegisterId?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._dutyPeriodsService
            .getAll(new GetAllDutyPeriodsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.dutyPeriods.set(result.items));


        this._timePoliciesService
            .getAll(new GetAllTimePoliciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.timePolicies.set(result.items));

        this._overtimeTypesService
            .getAll(new GetAllOvertimeTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.overtimeTypes.set(result.items));

        if (!timeRegisterId) {
            this.timeRegister.set(new CreateOrEditTimeRegisterDto());
            this.loadDuties([]);
            this.modal().show();
        } else {
            this._timeRegistersService.getTimeRegisterForEdit(timeRegisterId).subscribe((result) => {
                this.timeRegister.set(result.timeRegister);
                this.loadDuties([result.timeRegister.dutyId]);
                this.modal().show();
            });
        }
    }

    // Only active duties are offered; the one already on the record stays listed even if made inactive since.
    private loadDuties(currentDutyIds: (number | undefined)[]): void {
        this._dutiesService
            .getAll(
                new GetAllDutiesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    isActive: true,
                    includeIds: currentDutyIds.filter((dutyId): dutyId is number => !!dutyId),
                }),
            )
            .subscribe((result) => this.duties.set(result.items));
    }

    onShown(): void {
        document.getElementById('TimeRegister_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._timeRegistersService
            .createOrEdit(this.timeRegister())
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

    updateField(field: keyof CreateOrEditTimeRegisterDto, value: any): void {
        const current = this.timeRegister();
        (current as any)[field] = value;
        const next = new CreateOrEditTimeRegisterDto();
        Object.assign(next, current);
        this.timeRegister.set(next);
    }

    updateDateField(field: keyof CreateOrEditTimeRegisterDto, value: string): void {
        const current = this.timeRegister();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTimeRegisterDto();
        Object.assign(next, current);
        this.timeRegister.set(next);
    }

    toDateTimeLocal(value?: DateTime): string {
        return value ? value.toFormat("yyyy-MM-dd'T'HH:mm") : '';
    }

    updateDateTimeField(field: keyof CreateOrEditTimeRegisterDto, value: string): void {
        const current = this.timeRegister();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTimeRegisterDto();
        Object.assign(next, current);
        this.timeRegister.set(next);
    }
}
