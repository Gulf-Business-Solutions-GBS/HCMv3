import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeDutyScheduleDto,
    EmployeeDto,
    EmployeeDutySchedulesServiceProxy,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllRosterDutySchedulesInput,
    RosterDutyScheduleDto,
    RosterDutySchedulesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeDutyScheduleModal',
    templateUrl: './create-or-edit-employee-duty-schedule-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeDutyScheduleModalComponent extends AppComponentBase {
    private _service = inject(EmployeeDutySchedulesServiceProxy);
    private _rosterDutySchedulesService = inject(RosterDutySchedulesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeDutyScheduleDto>(new CreateOrEditEmployeeDutyScheduleDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeDutySchedule',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeId: this.record().employeeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly rosterDutySchedules = signal<RosterDutyScheduleDto[]>([]);

    /**
     * True when opened from the all-employees Duty Schedule screen to add a record: no employee is known
     * yet, so the form asks for one. From the employee record the employee is fixed and this stays false.
     */
    readonly pickEmployee = signal<boolean>(false);
    readonly employees = signal<EmployeeDto[]>([]);

    show(employeeId?: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);
        this.pickEmployee.set(!id && !employeeId);

        this.loadLookups();

        if (this.pickEmployee()) {
            this._employeesService
                .getAll(
                    new GetAllEmployeesInput({
                        maxResultCount: 1000,
                        skipCount: 0,
                        sorting: undefined,
                        filter: undefined,
                    })
                )
                .subscribe((result) => this.employees.set(result.items));
        }

        if (!id) {
            const record = new CreateOrEditEmployeeDutyScheduleDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeDutyScheduleForEdit(id).subscribe((result) => {
                this.record.set(result.employeeDutySchedule);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._rosterDutySchedulesService
            .getAll(
                new GetAllRosterDutySchedulesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.rosterDutySchedules.set(result.items));
    }

    onShown(): void {
        document
            .getElementById(this.pickEmployee() ? 'EmployeeDutySchedule_EmployeeId' : 'EmployeeDutySchedule_StartDate')
            ?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeDutyScheduleDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeDutyScheduleDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateEndDate(value: string): void {
        this.updateField('endDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
