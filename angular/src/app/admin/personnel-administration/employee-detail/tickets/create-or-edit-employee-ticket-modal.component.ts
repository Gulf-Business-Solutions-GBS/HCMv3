import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CalculateEmployeeTicketAmountInput,
    CreateOrEditEmployeeTicketDto,
    EmployeeTicketsServiceProxy,
    GetAllTicketTypesInput,
    GetEmployeeTicketEmployeesInput,
    SimpleLookupDto,
    TicketPaymentType,
    TicketTypeDto,
    TicketTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { SelectModule } from 'primeng/select';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { RecordDirectlyCheckComponent } from '@app/shared/common/record-directly/record-directly-check.component';
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';

@Component({
    selector: 'createOrEditEmployeeTicketModal',
    templateUrl: './create-or-edit-employee-ticket-modal.component.html',
    imports: [
        RecordDirectlyCheckComponent,
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        AttachmentsComponent,
        SelectModule,
    ],
})
export class CreateOrEditEmployeeTicketModalComponent extends AppComponentBase {
    private _service = inject(EmployeeTicketsServiceProxy);
    private _ticketTypesService = inject(TicketTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeTicketDto>(new CreateOrEditEmployeeTicketDto());

    readonly ticketTypes = signal<TicketTypeDto[]>([]);

    readonly ticketPaymentTypes = [
        { value: TicketPaymentType.PayWithPayroll, name: 'PayWithPayroll' },
        { value: TicketPaymentType.PayInAdvance, name: 'PayInAdvance' },
    ];

    /**
     * True when opened from the Employee Tickets list (no employee given): the employee is chosen here, as on
     * legacy's own Ticket screen. False inside an employee's record, where the employee is fixed.
     */
    readonly choosesEmployee = signal<boolean>(false);

    readonly employees = signal<SimpleLookupDto[]>([]);

    /** The dropdown's entries, "code - name", searchable inside the dropdown itself. */
    readonly employeeOptions = computed(() =>
        this.employees().map((e) => ({ id: e.id, label: `${e.code} - ${e.nameEn}` }))
    );

    show(employeeId: number | undefined, id?: number): void {
        this.choosesEmployee.set(!employeeId);
        this.active.set(true);

        if (this.choosesEmployee()) {
            this._service
                .getEmployeesForLookup(new GetEmployeeTicketEmployeesInput({ filter: undefined }))
                .subscribe((result) => this.employees.set(result.items));
        }

        this._ticketTypesService
            .getAll(
                new GetAllTicketTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.ticketTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeTicketDto();
            record.employeeId = employeeId;
            record.ticketPaymentType = TicketPaymentType.PayWithPayroll;
            // Legacy defaulted the payment date to today.
            record.paymentDate = DateTime.now().startOf('day');
            record.compensate = 0;
            record.amount = 0;
            record.advancedTicketPaid = 0;
            record.overrideAmount = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeTicketForEdit(id).subscribe((result) => {
                this.record.set(result.employeeTicket);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById(this.choosesEmployee() ? 'EmployeeTicket_EmployeeId' : 'EmployeeTicket_TicketTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeTicketDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeTicketDto();
        Object.assign(next, current);
        this.record.set(next);

        if (field === 'employeeId' || field === 'ticketTypeId' || field === 'paymentDate' || field === 'compensate') {
            this.calculateAmount();
        }
    }

    /**
     * Fills Amount (and the calculated Override Amount flag) from the server, as legacy did on the form.
     * Amount 0 is sent so ActualAmount pay types start from the sub-group's MaxAmount; save recalculates.
     */
    private calculateAmount(): void {
        const record = this.record();
        if (!record.employeeId || !record.ticketTypeId || !record.paymentDate || !record.compensate) {
            return;
        }

        this._service
            .calculateAmount(
                new CalculateEmployeeTicketAmountInput({
                    employeeId: record.employeeId,
                    ticketTypeId: record.ticketTypeId,
                    paymentDate: record.paymentDate,
                    compensate: record.compensate,
                    amount: 0,
                })
            )
            .subscribe((result) => {
                const next = new CreateOrEditEmployeeTicketDto();
                Object.assign(next, this.record());
                next.amount = result.amount;
                next.overrideAmount = result.overrideAmount;
                this.record.set(next);
            });
    }

    updateDateField(field: keyof CreateOrEditEmployeeTicketDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeTicketDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
