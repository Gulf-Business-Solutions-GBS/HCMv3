import { Component, input, model } from '@angular/core';
import { ControlContainer, FormsModule, NgForm } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditEmployeeBasicPayDto, PayInterval, PayType } from '@shared/service-proxies/service-proxies';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

/**
 * A basic pay's own fields, shared by the Create/Edit modal and the basic pay detail view.
 * Laid out as in the legacy HRMS: Effective Date | Pay Type, then Total Amount | Pay Interval.
 * End Date and Employee Action are hidden there, so they are not shown here either (their values are kept).
 *
 * It holds no form of its own: the controls register with the host's `<form #...="ngForm">` through
 * the view provider below, so the host's Save button sees their validity.
 */
@Component({
    selector: 'employeeBasicPayForm',
    templateUrl: './employee-basic-pay-form.component.html',
    viewProviders: [{ provide: ControlContainer, useExisting: NgForm }],
    imports: [DateInputDirective, FormsModule, ValidationMessagesComponent, LocalizePipe],
})
export class EmployeeBasicPayFormComponent extends AppComponentBase {
    readonly record = model.required<CreateOrEditEmployeeBasicPayDto>();
    readonly readOnly = input<boolean>(false);
    /** Sum of the Basic Pay Detail lines; read-only, as in the legacy HRMS. Blank for a new basic pay. */
    readonly totalAmount = input<number | undefined>(undefined);

    readonly payTypes = [
        { value: PayType.None, name: 'None' },
        { value: PayType.Regular, name: 'Regular' },
    ];

    readonly payIntervals = [
        { value: PayInterval.Monthly, name: 'Monthly' },
        { value: PayInterval.Daily, name: 'Daily' },
        { value: PayInterval.Hourly, name: 'Hourly' },
        { value: PayInterval.EndOfService, name: 'EndOfService' },
        { value: PayInterval.Yearly, name: 'Yearly' },
    ];

    updateField(field: keyof CreateOrEditEmployeeBasicPayDto, value: any): void {
        const next = new CreateOrEditEmployeeBasicPayDto();
        Object.assign(next, this.record());
        (next as any)[field] = value;
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeBasicPayDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
