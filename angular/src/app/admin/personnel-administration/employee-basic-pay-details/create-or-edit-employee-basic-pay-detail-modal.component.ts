import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BasicPayWageTypeLookupDto,
    CreateOrEditEmployeeBasicPayDetailDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    DependantUnit,
    EmployeeBasicPayDetailsServiceProxy,
    EmployeeBasicPayDto,
    EmployeeBasicPaysServiceProxy,
    GetAllCurrenciesInput,
    GetAllEmployeeBasicPaysInput,
    GetSelectableBasicPayWageTypesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { forkJoin, of } from 'rxjs';
import { finalize, map, switchMap } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';

@Component({
    selector: 'createOrEditEmployeeBasicPayDetailModal',
    templateUrl: './create-or-edit-employee-basic-pay-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class CreateOrEditEmployeeBasicPayDetailModalComponent extends AppComponentBase {
    private _service = inject(EmployeeBasicPayDetailsServiceProxy);
    private _employeeBasicPaysService = inject(EmployeeBasicPaysServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeBasicPayDetailDto>(new CreateOrEditEmployeeBasicPayDetailDto());
    /** Set when opened from a basic pay row: the header is fixed, so its dropdown is hidden. */
    readonly presetEmployeeBasicPayId = signal<number | undefined>(undefined);

    readonly employeeBasicPays = signal<EmployeeBasicPayDto[]>([]);
    /**
     * Legacy HRMS lookup, filtered by the backend: active earning wage types included in basic pay, and not
     * arrear or calculated ones. The line's own wage type is always included, so an older line never shows blank.
     */
    readonly wageTypes = signal<BasicPayWageTypeLookupDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);

    /** True while the record and its dropdown lists are loading; the dialog opens only once all have arrived. */
    readonly loading = signal<boolean>(false);

    show(id?: number, employeeBasicPayId?: number): void {
        this.presetEmployeeBasicPayId.set(employeeBasicPayId || undefined);

        if (!this.presetEmployeeBasicPayId()) {
            this._employeeBasicPaysService
                .getAll(
                    new GetAllEmployeeBasicPaysInput({
                        employeeId: undefined,
                        maxResultCount: 1000,
                        skipCount: 0,
                        sorting: undefined,
                    })
                )
                .subscribe((result) => this.employeeBasicPays.set(result.items));
        }

        const record$ = id
            ? this._service.getEmployeeBasicPayDetailForEdit(id).pipe(map((result) => result.employeeBasicPayDetail))
            : of(this.newRecord());

        // Load the record first, then its lists, and open the dialog only when every field can show its value.
        this.loading.set(true);
        record$
            .pipe(
                switchMap((record) =>
                    forkJoin({
                        wageTypes: this._service.getSelectableWageTypes(
                            new GetSelectableBasicPayWageTypesInput({ currentWageTypeId: record.wageTypeId })
                        ),
                        currencies: this._currenciesService.getAll(
                            new GetAllCurrenciesInput({
                                maxResultCount: 1000,
                                skipCount: 0,
                                sorting: undefined,
                                filter: undefined,
                            })
                        ),
                    }).pipe(map((lists) => ({ record, ...lists })))
                ),
                finalize(() => this.loading.set(false))
            )
            .subscribe(({ record, wageTypes, currencies }) => {
                this.wageTypes.set(wageTypes);
                this.currencies.set(currencies.items);
                this.record.set(record);
                this.active.set(true);
                this.modal().show();
            });
    }

    private newRecord(): CreateOrEditEmployeeBasicPayDetailDto {
        const record = new CreateOrEditEmployeeBasicPayDetailDto();
        record.amount = 0;
        record.dependantNumber = 0;
        record.dependantUnit = DependantUnit.None;
        record.employeeBasicPayId = this.presetEmployeeBasicPayId();
        return record;
    }

    onShown(): void {
        const firstFieldId = this.presetEmployeeBasicPayId()
            ? 'EmployeeBasicPayDetail_WageTypeId'
            : 'EmployeeBasicPayDetail_EmployeeBasicPayId';
        document.getElementById(firstFieldId)?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('SuccessfullySaved'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateField(field: keyof CreateOrEditEmployeeBasicPayDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeBasicPayDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof CreateOrEditEmployeeBasicPayDetailDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
