import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BillingMethod,
    BillingType,
    Category,
    CreateOrEditWageTypeDto,
    GetAllWageTypesInput,
    ProrateMethod,
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
    selector: 'createOrEditWageTypeModal',
    templateUrl: './create-or-edit-wage-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditWageTypeModalComponent extends AppComponentBase {
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly wageType = signal<CreateOrEditWageTypeDto>(new CreateOrEditWageTypeDto());
    readonly arrearSourceWageTypes = signal<WageTypeDto[]>([]);

    readonly billingTypes = [
        { value: BillingType.Earning, name: 'Earning' },
        { value: BillingType.Deduction, name: 'Deduction' },
        { value: BillingType.TimeType, name: 'TimeType' },
        { value: BillingType.Provision, name: 'Provision' },
    ];

    readonly billingMethods = [
        { value: BillingMethod.FixedAmountPerMonth, name: 'FixedAmountPerMonth' },
        { value: BillingMethod.FixedDaysPerMonth, name: 'FixedDaysPerMonth' },
        { value: BillingMethod.CalendarDaysPerMonth, name: 'CalendarDaysPerMonth' },
        { value: BillingMethod.WorkingDaysPerMonth, name: 'WorkingDaysPerMonth' },
    ];

    readonly prorateMethods = [
        { value: ProrateMethod.WorkingDays, name: 'WorkingDays' },
        { value: ProrateMethod.WorkingDays_REST, name: 'WorkingDays_REST' },
        { value: ProrateMethod.ActiveDays, name: 'ActiveDays' },
    ];

    readonly categories = [
        { value: Category.Custom, name: 'Custom' },
        { value: Category.Technical, name: 'Technical' },
    ];

    readonly includeFlags: { field: keyof CreateOrEditWageTypeDto; key: string }[] = [
        { field: 'includeInIndemnity', key: 'IncludeInIndemnity' },
        { field: 'includeInPaidLeave', key: 'IncludeInPaidLeave' },
        { field: 'includeInBasicPay', key: 'IncludeInBasicPay' },
        { field: 'includeInArrearPay', key: 'IncludeInArrearPay' },
        { field: 'includeInCalculatedPay', key: 'IncludeInCalculatedPay' },
        { field: 'includeInAdvancePay', key: 'IncludeInAdvancePay' },
        { field: 'includeInOvertimePay', key: 'IncludeInOvertimePay' },
        { field: 'includeInFIPosting', key: 'IncludeInFIPosting' },
        { field: 'includeInCutoff', key: 'IncludeInCutoff' },
        { field: 'includeInDisciplinary', key: 'IncludeInDisciplinary' },
        { field: 'includeInVariablePay', key: 'IncludeInVariablePay' },
        { field: 'includeInSocialSecurity', key: 'IncludeInSocialSecurity' },
        { field: 'includeInPartialPay', key: 'IncludeInPartialPay' },
        { field: 'includeInLateEarlyDed', key: 'IncludeInLateEarlyDed' },
        { field: 'includeInServiceBonus', key: 'IncludeInServiceBonus' },
        { field: 'includeInFund', key: 'IncludeInFund' },
        { field: 'dontImpactNetPay', key: 'DontImpactNetPay' },
        { field: 'isSecured', key: 'IsSecured' },
    ];

    show(wageTypeId?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.arrearSourceWageTypes.set(result.items));

        if (!wageTypeId) {
            this.wageType.set(new CreateOrEditWageTypeDto());
            this.modal().show();
        } else {
            this._wageTypesService.getWageTypeForEdit(wageTypeId).subscribe((result) => {
                this.wageType.set(result.wageType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('WageType_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._wageTypesService
            .createOrEdit(this.wageType())
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

    getFlag(field: keyof CreateOrEditWageTypeDto): boolean {
        return !!(this.wageType() as any)[field];
    }

    updateField(field: keyof CreateOrEditWageTypeDto, value: any): void {
        const current = this.wageType();
        (current as any)[field] = value;
        const next = new CreateOrEditWageTypeDto();
        Object.assign(next, current);
        this.wageType.set(next);
    }
}
