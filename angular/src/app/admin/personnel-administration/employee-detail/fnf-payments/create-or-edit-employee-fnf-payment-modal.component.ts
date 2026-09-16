import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeFnFPaymentDto,
    EmployeeActionDto,
    EmployeeActionsServiceProxy,
    EmployeeBasicPayDetailDto,
    EmployeeBasicPayDetailsServiceProxy,
    EmployeeFnFPaymentsServiceProxy,
    GetAllEmployeeActionsInput,
    GetAllEmployeeBasicPayDetailsInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeFnFPaymentModal',
    templateUrl: './create-or-edit-employee-fnf-payment-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe, LuxonFormatPipe],
})
export class CreateOrEditEmployeeFnFPaymentModalComponent extends AppComponentBase {
    private _service = inject(EmployeeFnFPaymentsServiceProxy);
    private _employeeActionsService = inject(EmployeeActionsServiceProxy);
    private _employeeBasicPayDetailsService = inject(EmployeeBasicPayDetailsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeFnFPaymentDto>(new CreateOrEditEmployeeFnFPaymentDto());

    readonly employeeActions = signal<EmployeeActionDto[]>([]);
    readonly employeeBasicPayDetails = signal<EmployeeBasicPayDetailDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._employeeActionsService
            .getAll(new GetAllEmployeeActionsInput({ employeeId, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeActions.set(result.items));

        // EmployeeBasicPayDetail is scoped by EmployeeBasicPayId (not directly by EmployeeId); no employee-scoped
        // filter is exposed by the backend, so the full lookup list is fetched here, consistent with the
        // EmployeeFundExemptions modal precedent (Wave 3 deferred bucket) for cross-parent dropdowns.
        this._employeeBasicPayDetailsService
            .getAll(new GetAllEmployeeBasicPayDetailsInput({ employeeBasicPayId: undefined, filter: undefined, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeBasicPayDetails.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeFnFPaymentDto();
            record.employeeId = employeeId;
            record.leaveCompensateDays = 0;
            record.gratuityCompensateDays = 0;
            record.indemnityCompensateAmount = 0;
            record.indemnityPayAdjustment = 0;
            record.ticketCompensateCount = 0;
            record.dependant1TicketCompensateCount = 0;
            record.dependant2TicketCompensateCount = 0;
            record.dependant3TicketCompensateCount = 0;
            record.compensateLoanAmount = 0;
            record.compensateBonusPay = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeFnFPaymentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeFnFPayment);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeFnFPayment_CompensationDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeFnFPaymentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeFnFPaymentDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeFnFPaymentDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeFnFPaymentDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
