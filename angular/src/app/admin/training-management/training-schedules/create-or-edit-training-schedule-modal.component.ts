import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTrainingScheduleDto,
    GetAllTrainingCoursesInput,
    TrainingCourseDto,
    TrainingCoursesServiceProxy,
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
    selector: 'createOrEditTrainingScheduleModal',
    templateUrl: './create-or-edit-training-schedule-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTrainingScheduleModalComponent extends AppComponentBase {
    private _service = inject(TrainingSchedulesServiceProxy);
    private _trainingCoursesService = inject(TrainingCoursesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditTrainingScheduleDto>(new CreateOrEditTrainingScheduleDto());

    readonly trainingCourses = signal<TrainingCourseDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._trainingCoursesService
            .getAll(
                new GetAllTrainingCoursesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.trainingCourses.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditTrainingScheduleDto());
            this.modal().show();
        } else {
            this._service.getTrainingScheduleForEdit(id).subscribe((result) => {
                this.record.set(result.trainingSchedule);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TrainingSchedule_Code')?.focus();
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

    updateField(field: keyof CreateOrEditTrainingScheduleDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditTrainingScheduleDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditTrainingScheduleDto, value: string): void {
        const current = this.record();
        const next = new CreateOrEditTrainingScheduleDto();
        Object.assign(next, current);
        (next as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        this.record.set(next);
    }
}
