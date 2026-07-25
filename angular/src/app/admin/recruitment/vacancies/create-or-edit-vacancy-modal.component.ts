import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditVacancyDto,
    GetAllOnbGroupsInput,
    GetAllSimpleLookupsInput,
    JobTitlesServiceProxy,
    OnbGroupDto,
    OnbGroupsServiceProxy,
    OrganizationUnitDto,
    OrganizationUnitServiceProxy,
    QualificationsServiceProxy,
    SimpleLookupDto,
    VacanciesServiceProxy,
    VacancyEmploymentType,
    VacancyStatus,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditVacancyModal',
    templateUrl: './create-or-edit-vacancy-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditVacancyModalComponent extends AppComponentBase {
    private _service = inject(VacanciesServiceProxy);
    private _jobTitlesService = inject(JobTitlesServiceProxy);
    private _qualificationsService = inject(QualificationsServiceProxy);
    private _organizationUnitService = inject(OrganizationUnitServiceProxy);
    private _onbGroupsService = inject(OnbGroupsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditVacancyDto>(new CreateOrEditVacancyDto());

    readonly jobTitles = signal<SimpleLookupDto[]>([]);
    readonly qualifications = signal<SimpleLookupDto[]>([]);
    readonly organizationUnits = signal<OrganizationUnitDto[]>([]);
    readonly onbGroups = signal<OnbGroupDto[]>([]);

    readonly vacancyStatuses = [
        { value: VacancyStatus.Open, name: 'Open' },
        { value: VacancyStatus.Filled, name: 'Filled' },
        { value: VacancyStatus.OnHold, name: 'OnHold' },
        { value: VacancyStatus.Dropped, name: 'Dropped' },
    ];

    readonly employmentTypes = [
        { value: VacancyEmploymentType.FullTime, name: 'FullTime' },
        { value: VacancyEmploymentType.PartTime, name: 'PartTime' },
    ];

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditVacancyDto();
            record.vacancyStatus = VacancyStatus.Open;
            record.employmentType = VacancyEmploymentType.FullTime;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getVacancyForEdit(id).subscribe((result) => {
                this.record.set(result.vacancy);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._jobTitlesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.jobTitles.set(result.items));

        this._qualificationsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.qualifications.set(result.items));

        this._organizationUnitService.getOrganizationUnits().subscribe((result) => this.organizationUnits.set(result.items ?? []));

        this._onbGroupsService
            .getAll(new GetAllOnbGroupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.onbGroups.set(result.items));
    }

    onShown(): void {
        document.getElementById('Vacancy_VacancyTitle')?.focus();
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

    updateField(field: keyof CreateOrEditVacancyDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditVacancyDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'requestionDate' | 'publishOn' | 'closingDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
