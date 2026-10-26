import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BanksServiceProxy,
    CreateOrEditBankDto,
    SimpleLookupDto,
    GetAllSimpleLookupsInput,
    BankFilesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditBankModal',
    templateUrl: './create-or-edit-bank-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditBankModalComponent extends AppComponentBase {
    private _bankService = inject(BanksServiceProxy);
    private _bankFilesService = inject(BankFilesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly bank = signal<CreateOrEditBankDto>(new CreateOrEditBankDto());
    readonly bankFiles = signal<SimpleLookupDto[]>([]);

    show(bankId?: number): void {
        this.active.set(true);

        this._bankFilesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.bankFiles.set(result.items);
            });

        if (!bankId) {
            this.bank.set(new CreateOrEditBankDto());
            this.modal().show();
        } else {
            this._bankService.getBankForEdit(bankId).subscribe((result) => {
                this.bank.set(result.bank);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Bank_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._bankService
            .createOrEdit(this.bank())
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
    updateField(field: keyof CreateOrEditBankDto, value: any): void {
        const current = this.bank();
        (current as any)[field] = value;
        const next = new CreateOrEditBankDto();
        Object.assign(next, current);
        this.bank.set(next);
    }
}
