import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CountriesServiceProxy,
    CreateOrEditEmployeeEducationDto,
    EducationBranchesServiceProxy,
    EducationsServiceProxy,
    EmployeeEducationsServiceProxy,
    GetAllInstitutionsInput,
    GetAllSimpleLookupsInput,
    InstitutionDto,
    InstitutionsServiceProxy,
    SimpleLookupDto,
    UniversitiesServiceProxy,
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
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';

@Component({
    selector: 'createOrEditEmployeeEducationModal',
    templateUrl: './create-or-edit-employee-education-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        AttachmentsComponent,
    ],
})
export class CreateOrEditEmployeeEducationModalComponent extends AppComponentBase {
    private _service = inject(EmployeeEducationsServiceProxy);
    private _educationsService = inject(EducationsServiceProxy);
    private _educationBranchesService = inject(EducationBranchesServiceProxy);
    private _universitiesService = inject(UniversitiesServiceProxy);
    private _institutionsService = inject(InstitutionsServiceProxy);
    private _countriesService = inject(CountriesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeEducationDto>(new CreateOrEditEmployeeEducationDto());

    readonly educations = signal<SimpleLookupDto[]>([]);
    readonly educationBranches = signal<SimpleLookupDto[]>([]);
    readonly universities = signal<SimpleLookupDto[]>([]);
    readonly institutions = signal<InstitutionDto[]>([]);
    readonly countries = signal<SimpleLookupDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeeEducationDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeEducationForEdit(id).subscribe((result) => {
                this.record.set(result.employeeEducation);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._educationsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.educations.set(result.items));

        this._educationBranchesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.educationBranches.set(result.items));

        this._universitiesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.universities.set(result.items));

        this._institutionsService
            .getAll(
                new GetAllInstitutionsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.institutions.set(result.items));

        this._countriesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.countries.set(result.items));
    }

    onShown(): void {
        document.getElementById('EmployeeEducation_EducationId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeEducationDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeEducationDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'startDate' | 'endDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
