import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeLoanExemptionDto,
    EmployeeLoanDto,
    EmployeeLoanExemptionsServiceProxy,
    EmployeeLoansServiceProxy,
    GetAllEmployeeLoansInput,
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
    selector: 'createOrEditEmployeeLoanExemptionModal',
    templateUrl: './create-or-edit-employee-loan-exemption-modal.component.html',
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
export class CreateOrEditEmployeeLoanExemptionModalComponent extends AppComponentBase {
    private _service = inject(EmployeeLoanExemptionsServiceProxy);
    private _employeeLoansService = inject(EmployeeLoansServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeLoanExemptionDto>(new CreateOrEditEmployeeLoanExemptionDto());

    readonly employeeLoans = signal<EmployeeLoanDto[]>([]);

    /** Set when opened from inside one loan (employee record > Loans): the loan cannot be changed there. */
    readonly loanLocked = signal<boolean>(false);

    /**
     * @param loanContext Opened from inside a loan: the loan list is that employee's only and a new
     * exemption starts on that loan.
     */
    show(id?: number, loanContext?: { employeeLoanId: number; employeeId: number }): void {
        this.active.set(true);
        this.loanLocked.set(!!loanContext);

        this._employeeLoansService
            .getAll(
                new GetAllEmployeeLoansInput({
                    employeeId: loanContext?.employeeId,
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                })
            )
            .subscribe((result) => this.employeeLoans.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeLoanExemptionDto();
            record.employeeLoanId = loanContext?.employeeLoanId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeLoanExemptionForEdit(id).subscribe((result) => {
                this.record.set(result.employeeLoanExemption);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeLoanExemption_EmployeeLoanId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeLoanExemptionDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeLoanExemptionDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeLoanExemptionDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
