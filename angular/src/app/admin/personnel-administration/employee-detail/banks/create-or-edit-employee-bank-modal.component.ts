import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BankDto,
    BanksServiceProxy,
    CreateOrEditEmployeeBankDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    CountriesServiceProxy,
    EmployeeBanksServiceProxy,
    GetAllBanksInput,
    GetAllCurrenciesInput,
    GetAllSimpleLookupsInput,
    PaymentType,
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
    selector: 'createOrEditEmployeeBankModal',
    templateUrl: './create-or-edit-employee-bank-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeBankModalComponent extends AppComponentBase {
    private _service = inject(EmployeeBanksServiceProxy);
    private _banksService = inject(BanksServiceProxy);
    private _countriesService = inject(CountriesServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeBankDto>(new CreateOrEditEmployeeBankDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeBank',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeId: this.record().employeeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly banks = signal<BankDto[]>([]);
    readonly countries = signal<SimpleLookupDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);

    readonly paymentTypes = [
        { value: PaymentType.Bank, name: 'Bank' },
        { value: PaymentType.Cash, name: 'Cash' },
        { value: PaymentType.Na, name: 'Na' },
    ];

    show(employeeId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeeBankDto();
            record.employeeId = employeeId;
            record.paymentType = PaymentType.Bank;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeBankForEdit(id).subscribe((result) => {
                this.record.set(result.employeeBank);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._banksService
            .getAll(new GetAllBanksInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.banks.set(result.items));

        this._countriesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.countries.set(result.items));

        this._currenciesService
            .getAll(new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.currencies.set(result.items));
    }

    onShown(): void {
        document.getElementById('EmployeeBank_BankId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeBankDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeBankDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeBankDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
