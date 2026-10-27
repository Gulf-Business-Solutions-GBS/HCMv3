import { Component, OnInit, inject, input, model, signal } from '@angular/core';
import { ControlContainer, FormsModule, NgForm } from '@angular/forms';
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
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/**
 * The wage type's own fields, shared by the Create/Edit modal and the wage type detail page.
 *
 * It holds no form of its own: the controls register with the host's `<form #...="ngForm">` through
 * the view provider below, so the host's Save button sees their validity exactly as it did when the
 * fields lived directly in the modal.
 */
@Component({
    selector: 'wageTypeForm',
    templateUrl: './wage-type-form.component.html',
    viewProviders: [{ provide: ControlContainer, useExisting: NgForm }],
    imports: [FormsModule, ValidationMessagesComponent, LocalizePipe],
})
export class WageTypeFormComponent extends AppComponentBase implements OnInit {
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly wageType = model.required<CreateOrEditWageTypeDto>();
    readonly readOnly = input<boolean>(false);

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

    ngOnInit(): void {
        this._wageTypesService
            .getAll(
                new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.arrearSourceWageTypes.set(result.items));
    }

    getFlag(field: keyof CreateOrEditWageTypeDto): boolean {
        return !!(this.wageType() as any)[field];
    }

    updateField(field: keyof CreateOrEditWageTypeDto, value: any): void {
        const next = new CreateOrEditWageTypeDto();
        Object.assign(next, this.wageType());
        (next as any)[field] = value;
        this.wageType.set(next);
    }
}
