import { Component, OnInit, inject, input, model, signal } from '@angular/core';
import { ControlContainer, FormsModule, NgForm } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceViolationRulesServiceProxy,
    AccrualPeriod,
    CreateOrEditAbsenceTypeDto,
    CustomStartMonth,
    GetAllSimpleLookupsInput,
    GetAllWageTypesInput,
    LeaveBasisPeriod,
    LeaveDurationType,
    LeaveEntitlement,
    LeaveGender,
    LeaveReligion,
    QuotaType,
    ResetFrequency,
    RestrictType,
    SimpleLookupDto,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';

/**
 * The absence type's own fields, shared by the Create/Edit modal and the absence type detail page.
 *
 * It holds no form of its own: the controls register with the host's `<form #...="ngForm">` through
 * the view provider below, so the host's Save button sees their validity exactly as it did when the
 * fields lived directly in the modal.
 */
@Component({
    selector: 'absenceTypeForm',
    templateUrl: './absence-type-form.component.html',
    viewProviders: [{ provide: ControlContainer, useExisting: NgForm }],
    imports: [FormsModule, ValidationMessagesComponent, LocalizePipe, AttachmentsComponent],
})
export class AbsenceTypeFormComponent extends AppComponentBase implements OnInit {
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _absenceViolationRulesService = inject(AbsenceViolationRulesServiceProxy);

    readonly absenceType = model.required<CreateOrEditAbsenceTypeDto>();
    readonly readOnly = input<boolean>(false);

    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly absenceViolationRules = signal<SimpleLookupDto[]>([]);

    readonly leaveBasisPeriods = [
        { value: LeaveBasisPeriod.CalendarDays, name: 'CalendarDays' },
        { value: LeaveBasisPeriod.FixedDays, name: 'FixedDays' },
    ];

    readonly accrualPeriods = [
        { value: AccrualPeriod.NoAccrual, name: 'NoAccrual' },
        { value: AccrualPeriod.CalendarYear, name: 'CalendarYear' },
        { value: AccrualPeriod.FiscalYear, name: 'FiscalYear' },
        { value: AccrualPeriod.WorkYear, name: 'WorkYear' },
        { value: AccrualPeriod.BiAnnual, name: 'BiAnnual' },
        { value: AccrualPeriod.PerEmployment, name: 'PerEmployment' },
        { value: AccrualPeriod.Custom, name: 'Custom' },
    ];

    readonly customStartMonths = [
        { value: CustomStartMonth.None, name: 'None' },
        { value: CustomStartMonth.January, name: 'January' },
        { value: CustomStartMonth.February, name: 'February' },
        { value: CustomStartMonth.March, name: 'March' },
        { value: CustomStartMonth.April, name: 'April' },
        { value: CustomStartMonth.May, name: 'May' },
        { value: CustomStartMonth.June, name: 'June' },
        { value: CustomStartMonth.July, name: 'July' },
        { value: CustomStartMonth.August, name: 'August' },
        { value: CustomStartMonth.September, name: 'September' },
        { value: CustomStartMonth.October, name: 'October' },
        { value: CustomStartMonth.November, name: 'November' },
        { value: CustomStartMonth.December, name: 'December' },
    ];

    readonly resetFrequencies = [
        { value: ResetFrequency.Daily, name: 'Daily' },
        { value: ResetFrequency.Weekly, name: 'Weekly' },
        { value: ResetFrequency.Monthly, name: 'Monthly' },
        { value: ResetFrequency.Quarterly, name: 'Quarterly' },
        { value: ResetFrequency.Annually, name: 'Annually' },
    ];

    readonly restrictTypes = [
        { value: RestrictType.Warning, name: 'Warning' },
        { value: RestrictType.Error, name: 'Error' },
    ];

    readonly leaveDurationTypes = [
        { value: LeaveDurationType.All, name: 'All' },
        { value: LeaveDurationType.FullDay, name: 'FullDay' },
        { value: LeaveDurationType.Partial, name: 'Partial' },
    ];

    readonly leaveGenders = [
        { value: LeaveGender.All, name: 'All' },
        { value: LeaveGender.Male, name: 'Male' },
        { value: LeaveGender.Female, name: 'Female' },
    ];

    readonly leaveReligions = [
        { value: LeaveReligion.All, name: 'All' },
        { value: LeaveReligion.Muslim, name: 'Muslim' },
        { value: LeaveReligion.NonMuslim, name: 'NonMuslim' },
    ];

    readonly quotaTypes = [
        { value: QuotaType.None, name: 'None' },
        { value: QuotaType.AnnualLeave, name: 'AnnualLeave' },
        { value: QuotaType.SickLeave, name: 'SickLeave' },
    ];

    readonly leaveEntitlements = [
        { value: LeaveEntitlement.Full, name: 'Full' },
        { value: LeaveEntitlement.Prorated, name: 'Prorated' },
    ];

    ngOnInit(): void {
        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        this._absenceViolationRulesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceViolationRules.set(result.items));
    }

    updateField(field: keyof CreateOrEditAbsenceTypeDto, value: any): void {
        const next = new CreateOrEditAbsenceTypeDto();
        Object.assign(next, this.absenceType());
        (next as any)[field] = value;
        this.absenceType.set(next);
    }
}
