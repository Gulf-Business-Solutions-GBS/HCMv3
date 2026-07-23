import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CitiesServiceProxy,
    CountriesServiceProxy,
    CreateOrEditEmployeeDto,
    EmployeeStatus,
    EmploymentType,
    Gender,
    GetAllCitiesInput,
    GetAllSimpleLookupsInput,
    GovernoratesServiceProxy,
    MaritalStatusesServiceProxy,
    NationalitiesServiceProxy,
    GetAllNationalitiesInput,
    QualificationsServiceProxy,
    RecruitmentTypesServiceProxy,
    ReligionsServiceProxy,
    SimpleLookupDto,
    TitlesServiceProxy,
    EmployeesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

interface LookupOption {
    id: number;
    nameEn: string | undefined;
}

@Component({
    selector: 'createOrEditEmployeeModal',
    templateUrl: './create-or-edit-employee-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeModalComponent extends AppComponentBase {
    private _employeesService = inject(EmployeesServiceProxy);
    private _nationalitiesService = inject(NationalitiesServiceProxy);
    private _religionsService = inject(ReligionsServiceProxy);
    private _maritalStatusesService = inject(MaritalStatusesServiceProxy);
    private _titlesService = inject(TitlesServiceProxy);
    private _governoratesService = inject(GovernoratesServiceProxy);
    private _countriesService = inject(CountriesServiceProxy);
    private _qualificationsService = inject(QualificationsServiceProxy);
    private _citiesService = inject(CitiesServiceProxy);
    private _recruitmentTypesService = inject(RecruitmentTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly employee = signal<CreateOrEditEmployeeDto>(new CreateOrEditEmployeeDto());

    readonly nationalities = signal<LookupOption[]>([]);
    readonly religions = signal<SimpleLookupDto[]>([]);
    readonly maritalStatuses = signal<SimpleLookupDto[]>([]);
    readonly titles = signal<SimpleLookupDto[]>([]);
    readonly governorates = signal<SimpleLookupDto[]>([]);
    readonly countries = signal<SimpleLookupDto[]>([]);
    readonly qualifications = signal<SimpleLookupDto[]>([]);
    readonly cities = signal<LookupOption[]>([]);
    readonly recruitmentTypes = signal<SimpleLookupDto[]>([]);

    readonly genders = [
        { value: Gender.Male, name: 'Male' },
        { value: Gender.Female, name: 'Female' },
    ];

    readonly statuses = [
        { value: EmployeeStatus.Active, name: 'Active' },
        { value: EmployeeStatus.Inactive, name: 'Inactive' },
    ];

    readonly employmentTypes = [
        { value: EmploymentType.Permanent, name: 'Permanent' },
        { value: EmploymentType.Temporary, name: 'Temporary' },
    ];

    show(employeeId?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!employeeId) {
            const employee = new CreateOrEditEmployeeDto();
            employee.status = EmployeeStatus.Active;
            employee.gender = Gender.Male;
            employee.employmentType = EmploymentType.Permanent;
            this.employee.set(employee);
            this.modal().show();
        } else {
            this._employeesService.getEmployeeForEdit(employeeId).subscribe((result) => {
                this.employee.set(result.employee);
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

        this._governoratesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.governorates.set(result.items));

        this._countriesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.countries.set(result.items));

        this._qualificationsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.qualifications.set(result.items));

        this._citiesService
            .getAll(new GetAllCitiesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.cities.set(result.items));

        this._recruitmentTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.recruitmentTypes.set(result.items));
    }

    onShown(): void {
        document.getElementById('Employee_FirstNameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._employeesService
            .createOrEdit(this.employee())
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

    updateField(field: keyof CreateOrEditEmployeeDto, value: any): void {
        const current = this.employee();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeDto();
        Object.assign(next, current);
        this.employee.set(next);
    }

    updateDateField(field: 'birthDate' | 'joiningDate', value: string): void {
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
            this.updateField('photo', reader.result as string);
        };
        reader.readAsDataURL(file);
    }
}
