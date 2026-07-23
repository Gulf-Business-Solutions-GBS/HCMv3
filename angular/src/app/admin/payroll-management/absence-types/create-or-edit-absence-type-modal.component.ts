import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypesServiceProxy,
    AbsenceViolationRulesServiceProxy,
    AccrualPeriod,
    CreateOrEditAbsenceTypeDto,
    CustomStartMonth,
    GetAllSimpleLookupsInput,
    GetAllWageTypesInput,
    LeaveBasisPeriod,
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
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditAbsenceTypeModal',
    templateUrl: './create-or-edit-absence-type-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAbsenceTypeModalComponent extends AppComponentBase {
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _absenceViolationRulesService = inject(AbsenceViolationRulesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly absenceType = signal<CreateOrEditAbsenceTypeDto>(new CreateOrEditAbsenceTypeDto());
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

    show(absenceTypeId?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        this._absenceViolationRulesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceViolationRules.set(result.items));

        if (!absenceTypeId) {
            this.absenceType.set(new CreateOrEditAbsenceTypeDto());
            this.modal().show();
        } else {
            this._absenceTypesService.getAbsenceTypeForEdit(absenceTypeId).subscribe((result) => {
                this.absenceType.set(result.absenceType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AbsenceType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._absenceTypesService
            .createOrEdit(this.absenceType())
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

    updateField(field: keyof CreateOrEditAbsenceTypeDto, value: any): void {
        const current = this.absenceType();
        (current as any)[field] = value;
        const next = new CreateOrEditAbsenceTypeDto();
        Object.assign(next, current);
        this.absenceType.set(next);
    }
}
