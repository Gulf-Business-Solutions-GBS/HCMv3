import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeLoanDto,
    EmployeeLoansServiceProxy,
    GetAllLoanTypesInput,
    LoanTypeDto,
    LoanTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';

@Component({
    selector: 'createOrEditEmployeeLoanModal',
    templateUrl: './create-or-edit-employee-loan-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeLoanModalComponent extends AppComponentBase {
    private _service = inject(EmployeeLoansServiceProxy);
    private _loanTypesService = inject(LoanTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeLoanDto>(new CreateOrEditEmployeeLoanDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeLoan',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeId: this.record().employeeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly loanTypes = signal<LoanTypeDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.isNew.set(!id);
        this.active.set(true);

        this._loanTypesService
            .getAll(new GetAllLoanTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.loanTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeLoanDto();
            record.employeeId = employeeId;
            record.actualAmount = 0;
            record.dueAmount = 0;
            record.fixedAmount = 0;
            record.recoverFracInLastTrans = false;
            record.includeInLoanPay = true;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeLoanForEdit(id).subscribe((result) => {
                this.record.set(result.employeeLoan);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeLoan_LoanTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeLoanDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeLoanDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeLoanDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeLoanDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
