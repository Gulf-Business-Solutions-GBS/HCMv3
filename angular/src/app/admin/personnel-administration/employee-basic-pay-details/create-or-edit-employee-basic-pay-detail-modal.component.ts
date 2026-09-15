import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeBasicPayDetailDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    DependantUnit,
    EmployeeBasicPayDetailsServiceProxy,
    EmployeeBasicPayDto,
    EmployeeBasicPaysServiceProxy,
    GetAllCurrenciesInput,
    GetAllEmployeeBasicPaysInput,
    GetAllWageTypesInput,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';

@Component({
    selector: 'createOrEditEmployeeBasicPayDetailModal',
    templateUrl: './create-or-edit-employee-basic-pay-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe, LuxonFormatPipe],
})
export class CreateOrEditEmployeeBasicPayDetailModalComponent extends AppComponentBase {
    private _service = inject(EmployeeBasicPayDetailsServiceProxy);
    private _employeeBasicPaysService = inject(EmployeeBasicPaysServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeBasicPayDetailDto>(new CreateOrEditEmployeeBasicPayDetailDto());
    /** Set when opened from a basic pay row: the header is fixed, so its dropdown is hidden. */
    readonly presetEmployeeBasicPayId = signal<number | undefined>(undefined);

    readonly employeeBasicPays = signal<EmployeeBasicPayDto[]>([]);
    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);

    readonly dependantUnits = [
        { value: DependantUnit.None, name: 'None' },
        { value: DependantUnit.Percentage, name: 'Percentage' },
    ];

    show(id?: number, employeeBasicPayId?: number): void {
        this.active.set(true);
        this.presetEmployeeBasicPayId.set(employeeBasicPayId || undefined);

        if (!this.presetEmployeeBasicPayId()) {
            this._employeeBasicPaysService
                .getAll(new GetAllEmployeeBasicPaysInput({ employeeId: undefined, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
                .subscribe((result) => this.employeeBasicPays.set(result.items));
        }

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        this._currenciesService
            .getAll(new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.currencies.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeBasicPayDetailDto();
            record.amount = 0;
            record.dependantNumber = 0;
            record.dependantUnit = DependantUnit.None;
            record.employeeBasicPayId = this.presetEmployeeBasicPayId();
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeBasicPayDetailForEdit(id).subscribe((result) => {
                this.record.set(result.employeeBasicPayDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        const firstFieldId = this.presetEmployeeBasicPayId() ? 'EmployeeBasicPayDetail_WageTypeId' : 'EmployeeBasicPayDetail_EmployeeBasicPayId';
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
