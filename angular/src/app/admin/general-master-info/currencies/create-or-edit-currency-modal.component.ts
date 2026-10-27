import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditCurrencyDto, CurrenciesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCurrencyModal',
    templateUrl: './create-or-edit-currency-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCurrencyModalComponent extends AppComponentBase {
    private _currenciesService = inject(CurrenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly currency = signal<CreateOrEditCurrencyDto>(new CreateOrEditCurrencyDto());

    show(currencyId?: number): void {
        this.active.set(true);

        if (!currencyId) {
            this.currency.set(new CreateOrEditCurrencyDto());
            this.modal().show();
        } else {
            this._currenciesService.getCurrencyForEdit(currencyId).subscribe((result) => {
                this.currency.set(result.currency);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Currency_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._currenciesService
            .createOrEdit(this.currency())
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
    updateField(field: keyof CreateOrEditCurrencyDto, value: any): void {
        const current = this.currency();
        (current as any)[field] = value;
        const next = new CreateOrEditCurrencyDto();
        Object.assign(next, current);
        this.currency.set(next);
    }
}
