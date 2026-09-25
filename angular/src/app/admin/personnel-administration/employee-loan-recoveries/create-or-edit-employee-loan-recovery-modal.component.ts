import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeLoanRecoveryDto,
    EmployeeLoanDto,
    EmployeeLoanRecoveriesServiceProxy,
    EmployeeLoansServiceProxy,
    GetAllEmployeeLoansInput,
    LoanRecoveryMethod,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeLoanRecoveryModal',
    templateUrl: './create-or-edit-employee-loan-recovery-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        LuxonFormatPipe,
    ],
})
export class CreateOrEditEmployeeLoanRecoveryModalComponent extends AppComponentBase {
    private _service = inject(EmployeeLoanRecoveriesServiceProxy);
    private _employeeLoansService = inject(EmployeeLoansServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeLoanRecoveryDto>(new CreateOrEditEmployeeLoanRecoveryDto());

    readonly employeeLoans = signal<EmployeeLoanDto[]>([]);

    readonly loanRecoveryMethods = [
        { value: LoanRecoveryMethod.RecoveryThroughPayroll, name: 'RecoveryThroughPayroll' },
        { value: LoanRecoveryMethod.RecoveryExternal, name: 'RecoveryExternal' },
        { value: LoanRecoveryMethod.RecoveryThroughEoS, name: 'RecoveryThroughEoS' },
        { value: LoanRecoveryMethod.RecoveryExemption, name: 'RecoveryExemption' },
        { value: LoanRecoveryMethod.AddlRecoveryThroughPayroll, name: 'AddlRecoveryThroughPayroll' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._employeeLoansService
            .getAll(new GetAllEmployeeLoansInput({ employeeId: undefined, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeLoans.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeLoanRecoveryDto();
            record.recoveryAmount = 0;
            record.loanRecoveryMethod = LoanRecoveryMethod.RecoveryThroughPayroll;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeLoanRecoveryForEdit(id).subscribe((result) => {
                this.record.set(result.employeeLoanRecovery);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeLoanRecovery_EmployeeLoanId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeLoanRecoveryDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeLoanRecoveryDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeLoanRecoveryDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeLoanRecoveryDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
