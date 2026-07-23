import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountInfoDto,
    AccountInfosServiceProxy,
    CostElementDto,
    CostElementsServiceProxy,
    CreateOrEditEmployeeWageAccountDto,
    EmployeeWageAccountsServiceProxy,
    GetAllAccountInfosInput,
    GetAllCostElementsInput,
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
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeWageAccountModal',
    templateUrl: './create-or-edit-employee-wage-account-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeWageAccountModalComponent extends AppComponentBase {
    private _service = inject(EmployeeWageAccountsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _accountInfosService = inject(AccountInfosServiceProxy);
    private _costElementsService = inject(CostElementsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeWageAccountDto>(new CreateOrEditEmployeeWageAccountDto());

    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly accountInfos = signal<AccountInfoDto[]>([]);
    readonly costElements = signal<CostElementDto[]>([]);

    readonly costElementFields: (keyof CreateOrEditEmployeeWageAccountDto)[] = [
        'costElement1Id',
        'costElement2Id',
        'costElement3Id',
        'costElement4Id',
        'costElement5Id',
        'costElement6Id',
        'costElement7Id',
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this.loadLookups();

        if (!id) {
            const record = new CreateOrEditEmployeeWageAccountDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeWageAccountForEdit(id).subscribe((result) => {
                this.record.set(result.employeeWageAccount);
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
        document.getElementById('EmployeeWageAccount_WageTypeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    getCostElementValue(field: keyof CreateOrEditEmployeeWageAccountDto): number | undefined {
        return (this.record() as any)[field];
    }

    updateField(field: keyof CreateOrEditEmployeeWageAccountDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeWageAccountDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
