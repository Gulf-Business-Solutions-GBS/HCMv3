import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PayeeBankDetailsServiceProxy,
    CreateOrEditPayeeBankDetailDto,
    BankDto,
    SimpleLookupDto,
    GetAllBanksInput,
    GetAllSimpleLookupsInput,
    BanksServiceProxy,
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
    selector: 'createOrEditPayeeBankDetailModal',
    templateUrl: './create-or-edit-payee-bank-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPayeeBankDetailModalComponent extends AppComponentBase {
    private _payeeBankDetailService = inject(PayeeBankDetailsServiceProxy);
    private _banksService = inject(BanksServiceProxy);
    private _bankFilesService = inject(BankFilesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly payeeBankDetail = signal<CreateOrEditPayeeBankDetailDto>(new CreateOrEditPayeeBankDetailDto());
    readonly banks = signal<BankDto[]>([]);
    readonly bankFiles = signal<SimpleLookupDto[]>([]);

    show(payeeBankDetailId?: number): void {
        this.active.set(true);

        this._banksService
            .getAll(new GetAllBanksInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.banks.set(result.items);
            });

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

        if (!payeeBankDetailId) {
            this.payeeBankDetail.set(new CreateOrEditPayeeBankDetailDto());
            this.modal().show();
        } else {
            this._payeeBankDetailService.getPayeeBankDetailForEdit(payeeBankDetailId).subscribe((result) => {
                this.payeeBankDetail.set(result.payeeBankDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PayeeBankDetail_BankCode')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._payeeBankDetailService
            .createOrEdit(this.payeeBankDetail())
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
    updateField(field: keyof CreateOrEditPayeeBankDetailDto, value: any): void {
        const current = this.payeeBankDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditPayeeBankDetailDto();
        Object.assign(next, current);
        this.payeeBankDetail.set(next);
    }
}
