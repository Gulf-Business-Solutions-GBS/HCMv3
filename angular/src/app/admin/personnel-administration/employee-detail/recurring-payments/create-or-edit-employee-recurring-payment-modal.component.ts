import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeRecurringPaymentDto,
    EmployeeRecurringPaymentsServiceProxy,
    GetAllWageTypesInput,
    GetEmployeeRecurringPaymentEmployeesInput,
    PayInterval,
    SimpleLookupDto,
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
import { SelectModule } from 'primeng/select';

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
        SelectModule,
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

    /**
     * True when the dialog was opened with no employee given -- from the Recurring Payments list rather than from
     * inside an employee's record -- so the employee is chosen here, on create and on edit.
     */
    readonly choosesEmployee = signal<boolean>(false);
    readonly employees = signal<SimpleLookupDto[]>([]);
    readonly employeeOptions = computed(() =>
        this.employees().map((e) => ({ id: e.id, label: `${e.code} - ${e.nameEn}` }))
    );

    /**
     * Legacy EmployeeBasicPayController removes Daily, Hourly and End of Service from this screen: payroll pays a
     * recurring payment only as Monthly (full amount every month it covers) or Yearly (full amount in the month
     * of its Start Date).
     */
    readonly payIntervals = [
        { value: PayInterval.Monthly, name: 'Monthly' },
        { value: PayInterval.Yearly, name: 'Yearly' },
    ];

    readonly endDateBeforeStartDate = computed(() => {
        const { startDate, endDate } = this.record();
        return !!startDate && !!endDate && endDate.startOf('day') < startDate.startOf('day');
    });

    show(employeeId: number | undefined, id?: number): void {
        this.choosesEmployee.set(!employeeId);
        this.active.set(true);

        if (this.choosesEmployee()) {
            this._service
                .getEmployeesForLookup(new GetEmployeeRecurringPaymentEmployeesInput({ filter: undefined }))
                .subscribe((result) => this.employees.set(result.items));
        }

        this._wageTypesService
            .getAll(
                new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!id) {
            // Legacy AfterConstruction: Start Date and End Date both default to today. Enter 31-Dec-9999 for an
            // open-ended payment; with "Enable effective date in recurring payment" on, the server then closes
            // the previous open-ended entry the day before this one starts.
            const record = new CreateOrEditEmployeeRecurringPaymentDto();
            const today = DateTime.now().startOf('day');
            record.employeeId = employeeId;
            record.startDate = today;
            record.endDate = today;
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
        if (!this.choosesEmployee()) {
            document.getElementById('EmployeeRecurringPayment_StartDate')?.focus();
        }
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
