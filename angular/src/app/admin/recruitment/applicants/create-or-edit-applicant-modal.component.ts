import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicantsServiceProxy,
    CountriesServiceProxy,
    CreateOrEditApplicantDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    Gender,
    GetAllNationalitiesInput,
    GetAllSimpleLookupsInput,
    JobTitlesServiceProxy,
    MaritalStatusesServiceProxy,
    NationalitiesServiceProxy,
    NationalityDto,
    QualificationsServiceProxy,
    ReligionsServiceProxy,
    SimpleLookupDto,
    TitlesServiceProxy,
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

import { toBase64Payload, toImageSrc } from '@shared/utils/image-data-url';

@Component({
    selector: 'createOrEditApplicantModal',
    templateUrl: './create-or-edit-applicant-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditApplicantModalComponent extends AppComponentBase {
    readonly toImageSrc = toImageSrc;

    private _service = inject(ApplicantsServiceProxy);
    private _nationalitiesService = inject(NationalitiesServiceProxy);
    private _religionsService = inject(ReligionsServiceProxy);
    private _maritalStatusesService = inject(MaritalStatusesServiceProxy);
    private _titlesService = inject(TitlesServiceProxy);
    private _countriesService = inject(CountriesServiceProxy);
    private _qualificationsService = inject(QualificationsServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditApplicantDto>(new CreateOrEditApplicantDto());

    readonly nationalities = signal<NationalityDto[]>([]);
    readonly religions = signal<SimpleLookupDto[]>([]);
    readonly maritalStatuses = signal<SimpleLookupDto[]>([]);
    readonly titles = signal<SimpleLookupDto[]>([]);
    readonly countries = signal<SimpleLookupDto[]>([]);
    readonly qualifications = signal<SimpleLookupDto[]>([]);
    readonly jobTitles = signal<SimpleLookupDto[]>([]);
    /** Onboarding coordinator and buddy are employees. */
    readonly employees = signal<EmployeeDto[]>([]);

    readonly genders = [
        { value: Gender.Male, name: 'Male' },
        { value: Gender.Female, name: 'Female' },
    ];

    show(applicantId?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!applicantId) {
            const record = new CreateOrEditApplicantDto();
            record.gender = Gender.Male;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getApplicantForEdit(applicantId).subscribe((result) => {
                this.record.set(result.applicant);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._nationalitiesService
            .getAll(new GetAllNationalitiesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.nationalities.set(result.items));

        this._religionsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.religions.set(result.items));

        this._maritalStatusesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.maritalStatuses.set(result.items));

        this._titlesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.titles.set(result.items));

        this._countriesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.countries.set(result.items));

        this._qualificationsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.qualifications.set(result.items));

        this._jobTitlesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.jobTitles.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    onShown(): void {
        document.getElementById('Applicant_FirstNameEn')?.focus();
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

    updateField(field: keyof CreateOrEditApplicantDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditApplicantDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'birthDate' | 'joiningDate' | 'probationEndDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    onPhotoSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files && input.files.length ? input.files[0] : null;
        if (!file) {
            return;
        }

        const reader = new FileReader();
        reader.onload = () => {
            this.updateField('photo', toBase64Payload(reader.result as string));
        };
        reader.readAsDataURL(file);
    }
}
