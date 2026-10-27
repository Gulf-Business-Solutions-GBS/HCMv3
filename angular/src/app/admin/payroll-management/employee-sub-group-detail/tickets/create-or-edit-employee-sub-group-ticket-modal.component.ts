import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccrualPeriod,
    CreateOrEditEmployeeSubGroupTicketDto,
    EmployeeSubGroupTicketsServiceProxy,
    FareType,
    GetAllTicketTypesInput,
    TicketTypeDto,
    TicketTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeSubGroupTicketModal',
    templateUrl: './create-or-edit-employee-sub-group-ticket-modal.component.html',
    imports: [DateInputDirective, AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSubGroupTicketModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSubGroupTicketsServiceProxy);
    private _ticketTypesService = inject(TicketTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSubGroupTicketDto>(new CreateOrEditEmployeeSubGroupTicketDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeSubGroupTicket',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({
                EmployeeSubGroupId: this.record().employeeSubGroupId,
                TicketTypeId: this.record().ticketTypeId,
            }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly ticketTypes = signal<TicketTypeDto[]>([]);

    readonly fareTypes = [
        { value: FareType.OneWay, name: 'OneWay' },
        { value: FareType.Return, name: 'Return' },
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

    show(employeeSubGroupId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

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
            const record = new CreateOrEditEmployeeSubGroupTicketDto();
            record.employeeSubGroupId = employeeSubGroupId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSubGroupTicketForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSubGroupTicket);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSubGroupTicket_TicketTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSubGroupTicketDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSubGroupTicketDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateEndDate(value: string): void {
        this.updateField('endDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
