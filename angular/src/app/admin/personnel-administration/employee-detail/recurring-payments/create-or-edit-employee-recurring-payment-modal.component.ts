import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeRecurringPaymentDto,
    EmployeeRecurringPaymentsServiceProxy,
    GetAllWageTypesInput,
    PayInterval,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeRecurringPaymentModal',
    templateUrl: './create-or-edit-employee-recurring-payment-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeRecurringPaymentModalComponent extends AppComponentBase {
    private _service = inject(EmployeeRecurringPaymentsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeRecurringPaymentDto>(new CreateOrEditEmployeeRecurringPaymentDto());

    readonly wageTypes = signal<WageTypeDto[]>([]);

    readonly payIntervals = [
        { value: PayInterval.Monthly, name: 'Monthly' },
        { value: PayInterval.Daily, name: 'Daily' },
        { value: PayInterval.Hourly, name: 'Hourly' },
        { value: PayInterval.EndOfService, name: 'EndOfService' },
        { value: PayInterval.Yearly, name: 'Yearly' },
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(
                new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!id) {
            // Open-ended by default (31-Dec-9999), as legacy did: with "Enable effective date in recurring
            // payment" on, the server closes the previous open-ended entry the day before this one starts.
            const record = new CreateOrEditEmployeeRecurringPaymentDto();
            record.employeeId = employeeId;
            record.startDate = DateTime.now().startOf('day');
            record.endDate = DateTime.fromISO('9999-12-31');
            record.amount = 0;
            record.payInterval = PayInterval.Monthly;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeRecurringPaymentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeRecurringPayment);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeRecurringPayment_WageTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeRecurringPaymentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeRecurringPaymentDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeRecurringPaymentDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeRecurringPaymentDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
