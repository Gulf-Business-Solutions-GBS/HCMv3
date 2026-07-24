import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSponsorDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    EmployeeSponsorsServiceProxy,
    GetAllCurrenciesInput,
    GetAllGovtJobTitlesInput,
    GetAllSimpleLookupsInput,
    GetAllSponsorsInput,
    GovtJobTitleDto,
    GovtJobTitlesServiceProxy,
    ResidencyType,
    SimpleLookupDto,
    SponsorDto,
    SponsorsServiceProxy,
    WorkVisaTypesServiceProxy,
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
    selector: 'createOrEditEmployeeSponsorModal',
    templateUrl: './create-or-edit-employee-sponsor-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSponsorModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSponsorsServiceProxy);
    private _sponsorsService = inject(SponsorsServiceProxy);
    private _govtJobTitlesService = inject(GovtJobTitlesServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);
    private _workVisaTypesService = inject(WorkVisaTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSponsorDto>(new CreateOrEditEmployeeSponsorDto());

    readonly sponsors = signal<SponsorDto[]>([]);
    readonly govtJobTitles = signal<GovtJobTitleDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);
    readonly workVisaTypes = signal<SimpleLookupDto[]>([]);

    readonly residencyTypes = [
        { value: ResidencyType.Company, name: 'Company' },
        { value: ResidencyType.NonCompany, name: 'NonCompany' },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._sponsorsService
            .getAll(new GetAllSponsorsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.sponsors.set(result.items));

        this._govtJobTitlesService
            .getAll(new GetAllGovtJobTitlesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, govtJobCategoryId: undefined, filter: undefined }))
            .subscribe((result) => this.govtJobTitles.set(result.items));

        this._currenciesService
            .getAll(new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.currencies.set(result.items));

        this._workVisaTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workVisaTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeSponsorDto();
            record.employeeId = employeeId;
            record.residencyType = ResidencyType.Company;
            record.contractSalary = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSponsorForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSponsor);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSponsor_SponsorId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSponsorDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSponsorDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeSponsorDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeSponsorDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
