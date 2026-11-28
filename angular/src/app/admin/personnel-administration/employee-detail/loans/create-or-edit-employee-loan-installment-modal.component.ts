import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeLoanInstallmentDto,
    EmployeeLoanInstallmentsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

/**
 * One row of a loan's instalment schedule: the amount recovered from a month onward. Always opened from inside
 * its loan, so the loan is fixed. The date picked is moved to the 1st of its month, which is the server's rule
 * (the effective date is the first of a month); the end date is never typed -- the server closes each row the
 * day before the next one.
 */
@Component({
    selector: 'createOrEditEmployeeLoanInstallmentModal',
    templateUrl: './create-or-edit-employee-loan-installment-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeLoanInstallmentModalComponent extends AppComponentBase {
    private _service = inject(EmployeeLoanInstallmentsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeLoanInstallmentDto>(new CreateOrEditEmployeeLoanInstallmentDto());

    show(employeeLoanId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditEmployeeLoanInstallmentDto();
            record.employeeLoanId = employeeLoanId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeLoanInstallmentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeLoanInstallment);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeLoanInstallment_StartDate')?.focus();
    }

    /** The date picker's value, yyyy-MM-dd. */
    startDateValue(): string {
        return this.record().startDate?.toISODate() ?? '';
    }

    /** Kept as typed while the field is being edited: moving it mid-typing would overwrite a half-typed year. */
    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : undefined);
    }

    /** Any day may be entered; on leaving the field it moves to the 1st of its month, the only date the server accepts. */
    snapStartDateToMonth(): void {
        const date = this.record().startDate;
        if (date?.isValid && date.day !== 1) {
            this.updateField('startDate', date.startOf('month'));
        }
    }

    save(): void {
        // Enter in the date field submits without a blur.
        this.snapStartDateToMonth();
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

    updateField(field: keyof CreateOrEditEmployeeLoanInstallmentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeLoanInstallmentDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
