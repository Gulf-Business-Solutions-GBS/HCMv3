import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTrainingAttendanceDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllTrainingSchedulesInput,
    TrainingAttendanceStatus,
    TrainingAttendancesServiceProxy,
    TrainingScheduleDto,
    TrainingSchedulesServiceProxy,
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

@Component({
    selector: 'createOrEditTrainingAttendanceModal',
    templateUrl: './create-or-edit-training-attendance-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTrainingAttendanceModalComponent extends AppComponentBase {
    private _service = inject(TrainingAttendancesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _trainingSchedulesService = inject(TrainingSchedulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditTrainingAttendanceDto>(new CreateOrEditTrainingAttendanceDto());

    readonly employees = signal<EmployeeDto[]>([]);
    readonly trainingSchedules = signal<TrainingScheduleDto[]>([]);

    readonly attendanceStatuses = [
        { value: TrainingAttendanceStatus.Present, name: 'Present' },
        { value: TrainingAttendanceStatus.Absent, name: 'Absent' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._trainingSchedulesService
            .getAll(
                new GetAllTrainingSchedulesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    trainingCourseId: undefined,
                })
            )
            .subscribe((result) => this.trainingSchedules.set(result.items));

        if (!id) {
            const record = new CreateOrEditTrainingAttendanceDto();
            record.attendance = TrainingAttendanceStatus.Present;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getTrainingAttendanceForEdit(id).subscribe((result) => {
                this.record.set(result.trainingAttendance);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TrainingAttendance_TrainingScheduleId')?.focus();
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

    updateField(field: keyof CreateOrEditTrainingAttendanceDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditTrainingAttendanceDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditTrainingAttendanceDto, value: string): void {
        const current = this.record();
        const next = new CreateOrEditTrainingAttendanceDto();
        Object.assign(next, current);
        (next as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        this.record.set(next);
    }
}
