import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeInsuranceDto,
    EmployeeDependantDto,
    EmployeeDependantsServiceProxy,
    EmployeeInsurancesServiceProxy,
    GetAllEmployeeDependantsInput,
    GetAllSimpleLookupsInput,
    InstallmentInterval,
    InsurancePlansServiceProxy,
    InsuranceProvidersServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';

@Component({
    selector: 'createOrEditEmployeeInsuranceModal',
    templateUrl: './create-or-edit-employee-insurance-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeInsuranceModalComponent extends AppComponentBase {
    private _service = inject(EmployeeInsurancesServiceProxy);
    private _insurancePlansService = inject(InsurancePlansServiceProxy);
    private _insuranceProvidersService = inject(InsuranceProvidersServiceProxy);
    private _employeeDependantsService = inject(EmployeeDependantsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeInsuranceDto>(new CreateOrEditEmployeeInsuranceDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeInsurance',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeId: this.record().employeeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly insurancePlans = signal<SimpleLookupDto[]>([]);
    readonly insuranceProviders = signal<SimpleLookupDto[]>([]);
    readonly employeeDependants = signal<EmployeeDependantDto[]>([]);

    readonly installmentIntervals = [
        { value: InstallmentInterval.Monthly, name: 'Monthly' },
        { value: InstallmentInterval.Yearly, name: 'Yearly' },
    ];

    show(employeeId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._insurancePlansService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.insurancePlans.set(result.items));

        this._insuranceProvidersService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.insuranceProviders.set(result.items));

        this._employeeDependantsService
            .getAll(new GetAllEmployeeDependantsInput({ employeeId, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeDependants.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeInsuranceDto();
            record.employeeId = employeeId;
            record.insuranceAmount = 0;
            record.companyInterval = InstallmentInterval.Monthly;
            record.companyContribution = 0;
            record.installmentCompanyDedAmount = 0;
            record.employeeInterval = InstallmentInterval.Monthly;
            record.employeeContribution = 0;
            record.installmentEmployeeDedAmount = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeInsuranceForEdit(id).subscribe((result) => {
                this.record.set(result.employeeInsurance);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeInsurance_InsurancePlanId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeInsuranceDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeInsuranceDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeInsuranceDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeInsuranceDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
