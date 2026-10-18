import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTrainingCourseDto,
    GetAllInstitutionsInput,
    InstitutionDto,
    InstitutionsServiceProxy,
    TrainingCourseSource,
    TrainingCoursesServiceProxy,
    TrainingDeliveryMethod,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditTrainingCourseModal',
    templateUrl: './create-or-edit-training-course-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditTrainingCourseModalComponent extends AppComponentBase {
    private _service = inject(TrainingCoursesServiceProxy);
    private _institutionsService = inject(InstitutionsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditTrainingCourseDto>(new CreateOrEditTrainingCourseDto());

    readonly deliveryMethods = [
        { value: TrainingDeliveryMethod.Online, name: 'Online' },
        { value: TrainingDeliveryMethod.Classroom, name: 'Classroom' },
    ];

    readonly sources = [
        { value: TrainingCourseSource.Internal, name: 'Internal' },
        { value: TrainingCourseSource.External, name: 'External' },
    ];

    readonly institutions = signal<InstitutionDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._institutionsService
            .getAll(new GetAllInstitutionsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.institutions.set(result.items));

        if (!id) {
            const record = new CreateOrEditTrainingCourseDto();
            record.deliveryMethod = TrainingDeliveryMethod.Online;
            record.source = TrainingCourseSource.Internal;
            record.fees = 0;
            record.active = true;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getTrainingCourseForEdit(id).subscribe((result) => {
                this.record.set(result.trainingCourse);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TrainingCourse_Code')?.focus();
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

    updateField(field: keyof CreateOrEditTrainingCourseDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditTrainingCourseDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof CreateOrEditTrainingCourseDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
