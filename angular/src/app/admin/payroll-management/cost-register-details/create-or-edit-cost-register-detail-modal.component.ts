import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountInfoDto,
    AccountInfosServiceProxy,
    CostElementDto,
    CostElementsServiceProxy,
    CostRegisterDetailsServiceProxy,
    CreateOrEditCostRegisterDetailDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    GetAllAccountInfosInput,
    GetAllCostElementsInput,
    GetAllCurrenciesInput,
    GetAllWageTypesInput,
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

@Component({
    selector: 'createOrEditCostRegisterDetailModal',
    templateUrl: './create-or-edit-cost-register-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCostRegisterDetailModalComponent extends AppComponentBase {
    private _costRegisterDetailsService = inject(CostRegisterDetailsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _accountInfosService = inject(AccountInfosServiceProxy);
    private _costElementsService = inject(CostElementsServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly costRegisterDetail = signal<CreateOrEditCostRegisterDetailDto>(new CreateOrEditCostRegisterDetailDto());

    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly accountInfos = signal<AccountInfoDto[]>([]);
    readonly costElements = signal<CostElementDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);

    readonly costElementFields: (keyof CreateOrEditCostRegisterDetailDto)[] = [
        'costElement1Id',
        'costElement2Id',
        'costElement3Id',
        'costElement4Id',
        'costElement5Id',
        'costElement6Id',
        'costElement7Id',
    ];

    show(costRegisterDetailId?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!costRegisterDetailId) {
            this.costRegisterDetail.set(new CreateOrEditCostRegisterDetailDto());
            this.modal().show();
        } else {
            this._costRegisterDetailsService.getCostRegisterDetailForEdit(costRegisterDetailId).subscribe((result) => {
                this.costRegisterDetail.set(result.costRegisterDetail);
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

        this._currenciesService
            .getAll(new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.currencies.set(result.items));
    }

    onShown(): void {
        document.getElementById('CostRegisterDetail_CostRegisterId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._costRegisterDetailsService
            .createOrEdit(this.costRegisterDetail())
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

    getCostElementValue(field: keyof CreateOrEditCostRegisterDetailDto): number | undefined {
        return (this.costRegisterDetail() as any)[field];
    }

    updateField(field: keyof CreateOrEditCostRegisterDetailDto, value: any): void {
        const current = this.costRegisterDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditCostRegisterDetailDto();
        Object.assign(next, current);
        this.costRegisterDetail.set(next);
    }
}
