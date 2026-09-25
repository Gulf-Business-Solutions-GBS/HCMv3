import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicantDto,
    ApplicantsServiceProxy,
    CreateOrEditOnbApplicantTasksDto,
    GetAllApplicantsInput,
    GetAllOnbTasksInput,
    OnbApplicantTasksServiceProxy,
    OnbTaskDto,
    OnbTasksServiceProxy,
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
    selector: 'createOrEditOnbApplicantTaskModal',
    templateUrl: './create-or-edit-onb-applicant-task-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditOnbApplicantTaskModalComponent extends AppComponentBase {
    private _service = inject(OnbApplicantTasksServiceProxy);
    private _applicantsService = inject(ApplicantsServiceProxy);
    private _onbTasksService = inject(OnbTasksServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditOnbApplicantTasksDto>(new CreateOrEditOnbApplicantTasksDto());

    readonly applicants = signal<ApplicantDto[]>([]);
    readonly onbTasks = signal<OnbTaskDto[]>([]);

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditOnbApplicantTasksDto());
            this.modal().show();
        } else {
            this._service.getOnbApplicantTasksForEdit(id).subscribe((result) => {
                this.record.set(result.onbApplicantTasks);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._applicantsService
            .getAll(new GetAllApplicantsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.applicants.set(result.items));

        this._onbTasksService
            .getAll(new GetAllOnbTasksInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, onbTemplateId: undefined, filter: undefined }))
            .subscribe((result) => this.onbTasks.set(result.items));
    }

    onShown(): void {
        document.getElementById('OnbApplicantTask_ApplicantId')?.focus();
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

    updateField(field: keyof CreateOrEditOnbApplicantTasksDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditOnbApplicantTasksDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'completedOn', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
