import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicantCVDto,
    ApplicantCVsServiceProxy,
    AppStatus,
    ApplicationStatusesServiceProxy,
    CreateOrEditApplicationStatusDto,
    GetAllApplicantCVsInput,
    GetAllVacanciesInput,
    VacanciesServiceProxy,
    VacancyDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditApplicationStatusModal',
    templateUrl: './create-or-edit-application-status-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditApplicationStatusModalComponent extends AppComponentBase {
    private _service = inject(ApplicationStatusesServiceProxy);
    private _vacanciesService = inject(VacanciesServiceProxy);
    private _applicantCVsService = inject(ApplicantCVsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditApplicationStatusDto>(new CreateOrEditApplicationStatusDto());

    readonly vacancies = signal<VacancyDto[]>([]);
    readonly applicantCVs = signal<ApplicantCVDto[]>([]);

    readonly appStatuses = [
        { value: AppStatus.Applied, name: 'Applied' },
        { value: AppStatus.Shortlisted, name: 'Shortlisted' },
        { value: AppStatus.Interviewed, name: 'Interviewed' },
        { value: AppStatus.JobOffer, name: 'JobOffer' },
        { value: AppStatus.Hired, name: 'Hired' },
        { value: AppStatus.Rejected, name: 'Rejected' },
        { value: AppStatus.Joined, name: 'Joined' },
    ];

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditApplicationStatusDto();
            record.appStatus = AppStatus.Applied;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getApplicationStatusForEdit(id).subscribe((result) => {
                this.record.set(result.applicationStatus);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._vacanciesService
            .getAll(new GetAllVacanciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.vacancies.set(result.items));

        this._applicantCVsService
            .getAll(new GetAllApplicantCVsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, applicantId: undefined }))
            .subscribe((result) => this.applicantCVs.set(result.items));
    }

    onShown(): void {
        document.getElementById('ApplicationStatus_VacancyId')?.focus();
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

    updateField(field: keyof CreateOrEditApplicationStatusDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditApplicationStatusDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
