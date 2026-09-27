import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeTicketDto,
    EmployeeTicketsServiceProxy,
    GetAllTicketTypesInput,
    TicketPaymentType,
    TicketTypeDto,
    TicketTypesServiceProxy,
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
import { RecordDirectlyCheckComponent } from '@app/shared/common/record-directly/record-directly-check.component';

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

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._ticketTypesService
            .getAll(new GetAllTicketTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.ticketTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeTicketDto();
            record.employeeId = employeeId;
            record.ticketPaymentType = TicketPaymentType.PayWithPayroll;
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
        document.getElementById('EmployeeTicket_TicketTypeId')?.focus();
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
    }

    updateDateField(field: keyof CreateOrEditEmployeeTicketDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeTicketDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
