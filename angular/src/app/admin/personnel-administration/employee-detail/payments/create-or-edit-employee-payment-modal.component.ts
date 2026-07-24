import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CostCenterDto,
    CostCentersServiceProxy,
    CreateOrEditEmployeePaymentDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    EmployeePaymentsServiceProxy,
    GetAllCostCentersInput,
    GetAllCurrenciesInput,
    GetAllWageTypesInput,
    UnitType,
    WageTypeDto,
    WageTypesServiceProxy,
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
    selector: 'createOrEditEmployeePaymentModal',
    templateUrl: './create-or-edit-employee-payment-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeePaymentModalComponent extends AppComponentBase {
    private _service = inject(EmployeePaymentsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);
    private _costCentersService = inject(CostCentersServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeePaymentDto>(new CreateOrEditEmployeePaymentDto());

    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);
    readonly costCenters = signal<CostCenterDto[]>([]);

    readonly unitTypes = [
        { value: UnitType.Amount, name: 'Amount' },
        { value: UnitType.Day, name: 'Day' },
        { value: UnitType.Hour, name: 'Hour' },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeePaymentDto();
            record.employeeId = employeeId;
            record.amount = 0;
            record.unitType = UnitType.Amount;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeePaymentForEdit(id).subscribe((result) => {
                this.record.set(result.employeePayment);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        this._currenciesService
            .getAll(new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.currencies.set(result.items));

        this._costCentersService
            .getAll(new GetAllCostCentersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.costCenters.set(result.items));
    }

    onShown(): void {
        document.getElementById('EmployeePayment_WageTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeePaymentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeePaymentDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeePaymentDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeePaymentDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
