import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeTrainingDto,
    EmployeeDto,
    EmployeeTrainingsServiceProxy,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllTrainingCoursesInput,
    GetAllTrainingSchedulesInput,
    TrainingCompletionStatus,
    TrainingCourseDto,
    TrainingCoursesServiceProxy,
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

@Component({
    selector: 'createOrEditEmployeeTrainingModal',
    templateUrl: './create-or-edit-employee-training-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeTrainingModalComponent extends AppComponentBase {
    private _service = inject(EmployeeTrainingsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _trainingCoursesService = inject(TrainingCoursesServiceProxy);
    private _trainingSchedulesService = inject(TrainingSchedulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeTrainingDto>(new CreateOrEditEmployeeTrainingDto());

    readonly employees = signal<EmployeeDto[]>([]);
    readonly trainingCourses = signal<TrainingCourseDto[]>([]);
    readonly allTrainingSchedules = signal<TrainingScheduleDto[]>([]);

    // Cascading: filter the schedule dropdown to the currently selected training course.
    readonly trainingSchedules = computed(() => {
        const courseId = this.record().trainingCourseId;
        return courseId ? this.allTrainingSchedules().filter((s) => s.trainingCourseId === courseId) : this.allTrainingSchedules();
    });

    readonly completionStatuses = [
        { value: TrainingCompletionStatus.Complete, name: 'Complete' },
        { value: TrainingCompletionStatus.Pending, name: 'Pending' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._trainingCoursesService
            .getAll(new GetAllTrainingCoursesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.trainingCourses.set(result.items));

        this._trainingSchedulesService
            .getAll(new GetAllTrainingSchedulesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, trainingCourseId: undefined }))
            .subscribe((result) => this.allTrainingSchedules.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeTrainingDto();
            record.completionStatus = TrainingCompletionStatus.Pending;
            record.score = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeTrainingForEdit(id).subscribe((result) => {
                this.record.set(result.employeeTraining);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeTraining_EmployeeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeTrainingDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeTrainingDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof CreateOrEditEmployeeTrainingDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }

    updateTrainingCourse(value: number | undefined): void {
        const current = this.record();
        const next = new CreateOrEditEmployeeTrainingDto();
        Object.assign(next, current);
        next.trainingCourseId = value as any;
        next.trainingScheduleId = undefined as any;
        this.record.set(next);
    }
}
