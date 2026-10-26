import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AttendanceTypeDto,
    AttendanceTypesServiceProxy,
    AttendanceUnit,
    CreateOrEditEmployeeAttendanceDto,
    EmployeeAttendancesServiceProxy,
    GetAllAttendanceTypesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { RecordDirectlyCheckComponent } from '@app/shared/common/record-directly/record-directly-check.component';

@Component({
    selector: 'createOrEditEmployeeAttendanceModal',
    templateUrl: './create-or-edit-employee-attendance-modal.component.html',
    imports: [
        RecordDirectlyCheckComponent,
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeAttendanceModalComponent extends AppComponentBase {
    private _service = inject(EmployeeAttendancesServiceProxy);
    private _attendanceTypesService = inject(AttendanceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeAttendanceDto>(new CreateOrEditEmployeeAttendanceDto());

    readonly attendanceTypes = signal<AttendanceTypeDto[]>([]);

    readonly attendanceUnits = [
        { value: AttendanceUnit.Period, name: 'Period' },
        { value: AttendanceUnit.Day, name: 'Day' },
        { value: AttendanceUnit.Hours, name: 'Hours' },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._attendanceTypesService
            .getAll(
                new GetAllAttendanceTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.attendanceTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeAttendanceDto();
            record.employeeId = employeeId;
            record.attendanceUnit = AttendanceUnit.Day;
            record.attendanceHours = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeAttendanceForEdit(id).subscribe((result) => {
                this.record.set(result.employeeAttendance);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeAttendance_AttendanceTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeAttendanceDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeAttendanceDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeAttendanceDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeAttendanceDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }

    getTimeValue(field: keyof CreateOrEditEmployeeAttendanceDto): string | undefined {
        const value = (this.record() as any)[field] as DateTime | undefined;
        return value?.toFormat('HH:mm:ss') ?? undefined;
    }

    updateTimeField(field: keyof CreateOrEditEmployeeAttendanceDto, value: string): void {
        if (!value) {
            this.updateField(field, undefined as any);
            return;
        }
        const [hour, minute, second] = value.split(':').map((part) => Number(part));
        const current = (this.record() as any)[field] as DateTime | undefined;
        const base = current ?? DateTime.now();
        this.updateField(field, base.set({ hour, minute, second: second || 0, millisecond: 0 }));
    }
}
