import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountInfoDto,
    AccountInfosServiceProxy,
    CostElementDto,
    CostElementsServiceProxy,
    CreateOrEditWageAccountDto,
    GetAllAccountInfosInput,
    GetAllCostElementsInput,
    GetAllWageTypesInput,
    WageAccountsServiceProxy,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';

@Component({
    selector: 'createOrEditWageAccountModal',
    templateUrl: './create-or-edit-wage-account-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWageAccountModalComponent extends AppComponentBase {
    private _wageAccountsService = inject(WageAccountsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _accountInfosService = inject(AccountInfosServiceProxy);
    private _costElementsService = inject(CostElementsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly wageAccount = signal<CreateOrEditWageAccountDto>(new CreateOrEditWageAccountDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'WageAccount',
            record: this.wageAccount,
            isNew: this.isNew,
            keys: () => ({ WageTypeId: this.wageAccount().wageTypeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly accountInfos = signal<AccountInfoDto[]>([]);
    readonly costElements = signal<CostElementDto[]>([]);

    readonly costElementFields: (keyof CreateOrEditWageAccountDto)[] = [
        'costElement1Id',
        'costElement2Id',
        'costElement3Id',
        'costElement4Id',
        'costElement5Id',
        'costElement6Id',
        'costElement7Id',
    ];

    show(wageAccountId?: number): void {
        this.isNew.set(!wageAccountId);
        this.active.set(true);

        this.loadLookups();

        if (!wageAccountId) {
            this.wageAccount.set(new CreateOrEditWageAccountDto());
            this.modal().show();
        } else {
            this._wageAccountsService.getWageAccountForEdit(wageAccountId).subscribe((result) => {
                this.wageAccount.set(result.wageAccount);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        this._accountInfosService
            .getAll(new GetAllAccountInfosInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.accountInfos.set(result.items));

        this._costElementsService
            .getAll(new GetAllCostElementsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.costElements.set(result.items));
    }

    onShown(): void {
        document.getElementById('WageAccount_WageTypeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._wageAccountsService
            .createOrEdit(this.wageAccount())
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

    getCostElementValue(field: keyof CreateOrEditWageAccountDto): number | undefined {
        return (this.wageAccount() as any)[field];
    }

    updateField(field: keyof CreateOrEditWageAccountDto, value: any): void {
        const current = this.wageAccount();
        (current as any)[field] = value;
        const next = new CreateOrEditWageAccountDto();
        Object.assign(next, current);
        this.wageAccount.set(next);
    }

    updateDateField(field: keyof CreateOrEditWageAccountDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
