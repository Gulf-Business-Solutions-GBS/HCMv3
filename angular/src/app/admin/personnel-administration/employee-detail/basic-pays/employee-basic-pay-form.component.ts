import { Component, effect, inject, input, model, signal } from '@angular/core';
import { ControlContainer, FormsModule, NgForm } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeBasicPayDto,
    EmployeeActionDto,
    EmployeeActionsServiceProxy,
    GetAllEmployeeActionsInput,
    PayInterval,
    PayType,
} from '@shared/service-proxies/service-proxies';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { DateTime } from 'luxon';

/**
 * A basic pay's own fields, shared by the Create/Edit modal and the basic pay detail view.
 *
 * It holds no form of its own: the controls register with the host's `<form #...="ngForm">` through
 * the view provider below, so the host's Save button sees their validity.
 */
@Component({
    selector: 'employeeBasicPayForm',
    templateUrl: './employee-basic-pay-form.component.html',
    viewProviders: [{ provide: ControlContainer, useExisting: NgForm }],
    imports: [FormsModule, ValidationMessagesComponent, LocalizePipe, LuxonFormatPipe],
})
export class EmployeeBasicPayFormComponent extends AppComponentBase {
    private _employeeActionsService = inject(EmployeeActionsServiceProxy);

    readonly record = model.required<CreateOrEditEmployeeBasicPayDto>();
    /** The employee whose actions fill the Employee Action dropdown. */
    readonly employeeId = input<number>();
    readonly readOnly = input<boolean>(false);

    readonly employeeActions = signal<EmployeeActionDto[]>([]);

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

    constructor() {
        super();
        effect(() => {
            const employeeId = this.employeeId();
            if (!employeeId) {
                return;
            }

            this._employeeActionsService
                .getAll(new GetAllEmployeeActionsInput({ employeeId, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
                .subscribe((result) => this.employeeActions.set(result.items));
        });
    }

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
