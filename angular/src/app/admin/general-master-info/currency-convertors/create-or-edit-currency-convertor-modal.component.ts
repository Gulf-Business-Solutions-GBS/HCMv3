import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CurrencyConvertorsServiceProxy,
    CreateOrEditCurrencyConvertorDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    GetAllCurrenciesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditCurrencyConvertorModal',
    templateUrl: './create-or-edit-currency-convertor-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditCurrencyConvertorModalComponent extends AppComponentBase {
    private _currencyConvertorsService = inject(CurrencyConvertorsServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly currencyConvertor = signal<CreateOrEditCurrencyConvertorDto>(new CreateOrEditCurrencyConvertorDto());
    readonly currencies = signal<CurrencyDto[]>([]);

    show(currencyConvertorId?: number): void {
        this.active.set(true);

        this._currenciesService
            .getAll(
                new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => {
                this.currencies.set(result.items);
            });

        if (!currencyConvertorId) {
            this.currencyConvertor.set(new CreateOrEditCurrencyConvertorDto());
            this.modal().show();
        } else {
            this._currencyConvertorsService.getCurrencyConvertorForEdit(currencyConvertorId).subscribe((result) => {
                this.currencyConvertor.set(result.currencyConvertor);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CurrencyConvertor_CalendarDate')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._currencyConvertorsService
            .createOrEdit(this.currencyConvertor())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditCurrencyConvertorDto, value: any): void {
        const current = this.currencyConvertor();
        (current as any)[field] = value;
        const next = new CreateOrEditCurrencyConvertorDto();
        Object.assign(next, current);
        this.currencyConvertor.set(next);
    }

    updateDateField(field: keyof CreateOrEditCurrencyConvertorDto, value: string): void {
        const current = this.currencyConvertor();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditCurrencyConvertorDto();
        Object.assign(next, current);
        this.currencyConvertor.set(next);
    }
}
