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
import { FormsModule, NgForm } from '@angular/forms';
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
    styleUrl: './create-or-edit-employee-modal.component.css',
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

    /**
     * The form's six groups, with the controls in each.
     *
     * The groups already existed as `<h6>` headings; all this adds is the ability to collapse them, so
     * 121 fields stop arriving as one scroll. The control lists exist for one reason: a collapsed
     * section can hide the empty required field that is keeping Save disabled, and a Save button that
     * is disabled for an invisible reason is worse than a long form.
     */
    private static readonly FormSections: { id: string; labelKey: string; controls: string[] }[] = [
        {
            id: 'names',
            labelKey: 'Names',
            controls: [
                'FirstNameEn', 'SecondNameEn', 'ThirdNameEn', 'LastNameEn',
                'FirstNameAr', 'SecondNameAr', 'ThirdNameAr', 'LastNameAr',
            ],
        },
        {
            id: 'personal',
            labelKey: 'PersonalInfo',
            controls: [
                'Code', 'TitleId', 'BirthDate', 'Gender', 'MaritalStatusId', 'NationalityId',
                'ReligionId', 'QualificationId', 'BloodGroup', 'CivilID', 'BirthPlaceId',
                'FatherNameEn', 'FatherNameAr', 'MotherNameEn', 'MotherNameAr',
            ],
        },
        {
            id: 'address',
            labelKey: 'Address',
            controls: [
                'FlatNumn', 'FloorNum', 'BuildingNum', 'StreetNum', 'BlockNum', 'LaneNumber',
                'PaciNumber', 'ZipCode', 'City', 'GovernorateId', 'CountryId',
                'CurrentAddress', 'PermanentAddress',
            ],
        },
        {
            id: 'contact',
            labelKey: 'Contact',
            controls: [
                'Telephone', 'Mobile', 'PermanentTelephone', 'Email', 'AlternateEmail',
                'LoginID', 'AccessID', 'BusinessLandline', 'BusinessExtension',
            ],
        },
        {
            id: 'emergency',
            labelKey: 'EmergencyContactSection',
            controls: [
                'EmergencyContact', 'EmergencyNumber', 'EmergencyRelation',
                'EmergencyContactAddress', 'CriticalIllness',
            ],
        },
        {
            id: 'employment',
            labelKey: 'Employment',
            controls: [
                'Status', 'EmploymentType', 'JoiningDate', 'ProbationDays',
                'RecruitmentTypeId', 'PreviousEmpCode', 'ReferenceCode',
            ],
        },
    ];

    /**
     * Which sections are expanded. Names and Personal open by default -- between them they hold every
     * required field, so the common path stays a straight run down the form rather than a hunt through
     * six collapsed panels.
     */
    readonly openSections = signal<Set<string>>(new Set(['names', 'personal']));

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
        { value: Gender.NotSpecified, name: 'GenderNotSpecified' },
    ];

    readonly statuses = [
        { value: EmployeeStatus.Active, name: 'Active' },
        { value: EmployeeStatus.Inactive, name: 'Inactive' },
    ];

    readonly employmentTypes = [
        { value: EmploymentType.Permanent, name: 'Permanent' },
        { value: EmploymentType.Temporary, name: 'Temporary' },
    ];

    isSectionOpen(id: string): boolean {
        return this.openSections().has(id);
    }

    toggleSection(id: string): void {
        const next = new Set(this.openSections());
        next.has(id) ? next.delete(id) : next.add(id);
        this.openSections.set(next);
    }

    /**
     * How many controls in a section are invalid. Returns 0 rather than null so the template's
     * `@if (...; as missing)` treats "nothing wrong" as falsy and shows no badge.
     *
     * Counts invalid regardless of touched state: on a new employee the required fields are untouched
     * AND invalid, and they are precisely the ones keeping Save disabled. Waiting for the user to touch
     * a field before admitting it is required would tell them nothing at the moment they are stuck.
     */
    sectionInvalidCount(form: NgForm, id: string): number {
        const section = CreateOrEditEmployeeModalComponent.FormSections.find((s) => s.id === id);
        if (!section || !form?.form) {
            return 0;
        }

        return section.controls.filter((name) => form.form.get(name)?.invalid).length;
    }

    /** Sections with something missing, for the hint that explains a disabled Save. */
    incompleteSections(form: NgForm): { id: string; labelKey: string }[] {
        return CreateOrEditEmployeeModalComponent.FormSections.filter(
            (s) => this.sectionInvalidCount(form, s.id) > 0
        ).map((s) => ({ id: s.id, labelKey: s.labelKey }));
    }

    /** Opens every section holding an invalid control, so the blocking field is on screen. */
    revealIncompleteSections(form: NgForm): void {
        const next = new Set(this.openSections());
        for (const section of this.incompleteSections(form)) {
            next.add(section.id);
        }
        this.openSections.set(next);
    }

    show(employeeId?: number): void {
        this.active.set(true);
        this.loadLookups();

        // Reset the panels each time the modal opens: carrying the last record's expansion over would
        // have somebody editing a different employee through whatever shape they left behind.
        this.openSections.set(new Set(['names', 'personal']));

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
