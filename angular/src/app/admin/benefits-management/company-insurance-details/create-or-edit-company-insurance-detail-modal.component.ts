import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CompanyInsuranceDetailsServiceProxy,
    CreateOrEditCompanyInsuranceDetailDto,
    EmployeeInsuranceDto,
    EmployeeInsurancesServiceProxy,
    GetAllEmployeeInsurancesInput,
    InstallmentType,
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
    selector: 'createOrEditCompanyInsuranceDetailModal',
    templateUrl: './create-or-edit-company-insurance-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditCompanyInsuranceDetailModalComponent extends AppComponentBase {
    private _service = inject(CompanyInsuranceDetailsServiceProxy);
    private _employeeInsurancesService = inject(EmployeeInsurancesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditCompanyInsuranceDetailDto>(new CreateOrEditCompanyInsuranceDetailDto());

    readonly employeeInsurances = signal<EmployeeInsuranceDto[]>([]);

    readonly installmentTypes = [
        { value: InstallmentType.ThroughPayroll, name: 'ThroughPayroll' },
        { value: InstallmentType.External, name: 'External' },
        { value: InstallmentType.EndOfService, name: 'EndOfService' },
        { value: InstallmentType.Exemption, name: 'Exemption' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._employeeInsurancesService
            .getAll(new GetAllEmployeeInsurancesInput({ employeeId: undefined, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeInsurances.set(result.items));

        if (!id) {
            const record = new CreateOrEditCompanyInsuranceDetailDto();
            record.installmentAmount = 0;
            record.installmentType = InstallmentType.ThroughPayroll;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getCompanyInsuranceDetailForEdit(id).subscribe((result) => {
                this.record.set(result.companyInsuranceDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CompanyInsuranceDetail_EmployeeInsuranceId')?.focus();
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

    updateField(field: keyof CreateOrEditCompanyInsuranceDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditCompanyInsuranceDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditCompanyInsuranceDetailDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditCompanyInsuranceDetailDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
