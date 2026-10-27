import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountInfoDto,
    AccountInfosServiceProxy,
    CostElementDto,
    CostElementsServiceProxy,
    CountriesServiceProxy,
    CreateOrEditPayrollAreaDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    GetAllAccountInfosInput,
    GetAllCostElementsInput,
    GetAllCurrenciesInput,
    GetAllNotificationConfigsInput,
    GetAllSimpleLookupsInput,
    NotificationConfigDto,
    NotificationConfigsServiceProxy,
    PayrollAreasServiceProxy,
    PayrollPeriodsServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPayrollAreaModal',
    templateUrl: './create-or-edit-payroll-area-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPayrollAreaModalComponent extends AppComponentBase {
    private _payrollAreasService = inject(PayrollAreasServiceProxy);
    private _payrollPeriodsService = inject(PayrollPeriodsServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);
    private _costElementsService = inject(CostElementsServiceProxy);
    private _accountInfosService = inject(AccountInfosServiceProxy);
    private _countriesService = inject(CountriesServiceProxy);
    private _notificationConfigsService = inject(NotificationConfigsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly payrollArea = signal<CreateOrEditPayrollAreaDto>(new CreateOrEditPayrollAreaDto());

    readonly payrollPeriods = signal<SimpleLookupDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);
    readonly costElements = signal<CostElementDto[]>([]);
    readonly accountInfos = signal<AccountInfoDto[]>([]);
    readonly countries = signal<SimpleLookupDto[]>([]);
    readonly notificationConfigs = signal<NotificationConfigDto[]>([]);

    show(payrollAreaId?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!payrollAreaId) {
            this.payrollArea.set(new CreateOrEditPayrollAreaDto());
            this.modal().show();
        } else {
            this._payrollAreasService.getPayrollAreaForEdit(payrollAreaId).subscribe((result) => {
                this.payrollArea.set(result.payrollArea);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._payrollPeriodsService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.payrollPeriods.set(result.items));

        this._currenciesService
            .getAll(
                new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.currencies.set(result.items));

        this._costElementsService
            .getAll(
                new GetAllCostElementsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.costElements.set(result.items));

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

        this._countriesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.countries.set(result.items));

        this._notificationConfigsService
            .getAll(
                new GetAllNotificationConfigsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.notificationConfigs.set(result.items));
    }

    onShown(): void {
        document.getElementById('PayrollArea_Name')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._payrollAreasService
            .createOrEdit(this.payrollArea())
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

    updateField(field: keyof CreateOrEditPayrollAreaDto, value: any): void {
        const current = this.payrollArea();
        (current as any)[field] = value;
        const next = new CreateOrEditPayrollAreaDto();
        Object.assign(next, current);
        this.payrollArea.set(next);
    }
}
