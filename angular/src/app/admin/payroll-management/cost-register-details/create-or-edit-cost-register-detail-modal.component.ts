import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountInfoDto,
    AccountInfosServiceProxy,
    CostRegisterDetailsServiceProxy,
    CreateOrEditCostRegisterDetailDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    GetAllAccountInfosInput,
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
import { CostElementSlotFieldsComponent } from '@app/shared/common/cost-element-slots/cost-element-slot-fields.component';

@Component({
    selector: 'createOrEditCostRegisterDetailModal',
    templateUrl: './create-or-edit-cost-register-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        CostElementSlotFieldsComponent,
    ],
})
export class CreateOrEditCostRegisterDetailModalComponent extends AppComponentBase {
    private _costRegisterDetailsService = inject(CostRegisterDetailsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _accountInfosService = inject(AccountInfosServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly costRegisterDetail = signal<CreateOrEditCostRegisterDetailDto>(new CreateOrEditCostRegisterDetailDto());

    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly accountInfos = signal<AccountInfoDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);

    /** Set when opened from a register's page: the line belongs to that register and the field is not offered. */
    readonly fixedCostRegisterId = signal<number | undefined>(undefined);

    show(costRegisterDetailId?: number, costRegisterId?: number): void {
        this.fixedCostRegisterId.set(costRegisterId);
        this.active.set(true);

        this.loadLookups();

        if (!costRegisterDetailId) {
            const record = new CreateOrEditCostRegisterDetailDto();
            record.costRegisterId = costRegisterId;
            this.costRegisterDetail.set(record);
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
            .getAll(
                new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.wageTypes.set(result.items));

        this._accountInfosService
            .getAll(
                new GetAllAccountInfosInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.accountInfos.set(result.items));

        this._currenciesService
            .getAll(
                new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.currencies.set(result.items));
    }

    onShown(): void {
        document
            .getElementById(
                this.fixedCostRegisterId() ? 'CostRegisterDetail_WageTypeId' : 'CostRegisterDetail_CostRegisterId'
            )
            ?.focus();
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

    updateField(field: keyof CreateOrEditCostRegisterDetailDto, value: any): void {
        const current = this.costRegisterDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditCostRegisterDetailDto();
        Object.assign(next, current);
        this.costRegisterDetail.set(next);
    }
}
