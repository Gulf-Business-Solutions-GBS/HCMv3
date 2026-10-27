import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeInsuranceDetailDto,
    EmployeeInsuranceDetailsServiceProxy,
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
    selector: 'createOrEditEmployeeInsuranceDetailModal',
    templateUrl: './create-or-edit-employee-insurance-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeInsuranceDetailModalComponent extends AppComponentBase {
    private _service = inject(EmployeeInsuranceDetailsServiceProxy);
    private _employeeInsurancesService = inject(EmployeeInsurancesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeInsuranceDetailDto>(new CreateOrEditEmployeeInsuranceDetailDto());

    readonly employeeInsurances = signal<EmployeeInsuranceDto[]>([]);

    readonly installmentTypes = [
        { value: InstallmentType.ThroughPayroll, name: 'ThroughPayroll' },
        { value: InstallmentType.External, name: 'External' },
        { value: InstallmentType.EndOfService, name: 'EndOfService' },
        { value: InstallmentType.Exemption, name: 'Exemption' },
    ];

    /** Set when opened from inside one insurance (employee record > Insurances): it cannot be changed there. */
    readonly insuranceLocked = signal<boolean>(false);

    /**
     * @param insuranceContext Opened from inside an insurance: the insurance list is that employee's only, a
     * new installment starts on that insurance, and it defaults to External -- through-payroll installments
     * are written by the payroll run and are not keyed in from there.
     */
    show(id?: number, insuranceContext?: { employeeInsuranceId: number; employeeId: number }): void {
        this.active.set(true);
        this.insuranceLocked.set(!!insuranceContext);

        this._employeeInsurancesService
            .getAll(
                new GetAllEmployeeInsurancesInput({
                    employeeId: insuranceContext?.employeeId,
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                })
            )
            .subscribe((result) => this.employeeInsurances.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeInsuranceDetailDto();
            record.installmentAmount = 0;
            record.employeeInsuranceId = insuranceContext?.employeeInsuranceId;
            record.installmentType = insuranceContext ? InstallmentType.External : InstallmentType.ThroughPayroll;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeInsuranceDetailForEdit(id).subscribe((result) => {
                this.record.set(result.employeeInsuranceDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeInsuranceDetail_EmployeeInsuranceId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeInsuranceDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeInsuranceDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeInsuranceDetailDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeInsuranceDetailDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
