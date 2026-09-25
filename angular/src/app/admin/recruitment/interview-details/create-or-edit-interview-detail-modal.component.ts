import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationStatusDto,
    ApplicationStatusesServiceProxy,
    CreateOrEditInterviewDetailsDto,
    GetAllApplicationStatusesInput,
    InterviewDetailsServiceProxy,
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
    selector: 'createOrEditInterviewDetailModal',
    templateUrl: './create-or-edit-interview-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditInterviewDetailModalComponent extends AppComponentBase {
    private _service = inject(InterviewDetailsServiceProxy);
    private _applicationStatusesService = inject(ApplicationStatusesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditInterviewDetailsDto>(new CreateOrEditInterviewDetailsDto());

    readonly applicationStatuses = signal<ApplicationStatusDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._applicationStatusesService
            .getAll(new GetAllApplicationStatusesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, vacancyId: undefined }))
            .subscribe((result) => this.applicationStatuses.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditInterviewDetailsDto());
            this.modal().show();
        } else {
            this._service.getInterviewDetailsForEdit(id).subscribe((result) => {
                this.record.set(result.interviewDetails);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('InterviewDetails_ApplicationStatusId')?.focus();
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

    updateField(field: keyof CreateOrEditInterviewDetailsDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditInterviewDetailsDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'interviewDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateTimeField(field: 'interviewTime', value: string): void {
        this.updateField(field, value ? DateTime.fromFormat(value, 'HH:mm') : (undefined as any));
    }
}
