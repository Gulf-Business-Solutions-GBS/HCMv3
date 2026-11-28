import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeLoanDto,
    EmployeeLoansServiceProxy,
    GetAllLoanTypesInput,
    GetEmployeeLoanEmployeesInput,
    LoanTypeDto,
    LoanTypesServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule, NgForm } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { RecordDirectlyCheckComponent } from '@app/shared/common/record-directly/record-directly-check.component';
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';
import { EmployeeLoanDetailsComponent } from './employee-loan-details.component';
import { SelectModule } from 'primeng/select';

@Component({
    selector: 'createOrEditEmployeeLoanModal',
    templateUrl: './create-or-edit-employee-loan-modal.component.html',
    imports: [
        RecordDirectlyCheckComponent,
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        AttachmentsComponent,
        EmployeeLoanDetailsComponent,
        SelectModule,
    ],
})
export class CreateOrEditEmployeeLoanModalComponent extends AppComponentBase {
    private _service = inject(EmployeeLoansServiceProxy);
    private _loanTypesService = inject(LoanTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly recordForm = viewChild<NgForm>('recordForm');
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

    /**
     * True when the dialog was opened with no employee given -- from the Employee Loans list rather than from
     * inside an employee's record -- so the employee is chosen here, on create and on edit (legacy lets the
     * loan's employee be changed).
     */
    readonly choosesEmployee = signal<boolean>(false);

    readonly employees = signal<SimpleLookupDto[]>([]);

    /** The dropdown's entries, "code - name", searchable inside the dropdown itself. */
    readonly employeeOptions = computed(() =>
        this.employees().map((e) => ({ id: e.id, label: `${e.code} - ${e.nameEn}` }))
    );

    /** The loan's employee as loaded for edit; always offered in the dropdown, and shown when it is read-only. */
    readonly currentEmployee = signal<SimpleLookupDto | undefined>(undefined);

    /** Read-only on the form, as in legacy: payroll and recoveries keep it. */
    readonly balanceDueAmount = signal<number | undefined>(undefined);

    /** Set when something was saved in the instalments / recoveries / exemptions tabs under the loan. */
    readonly detailsChanged = signal<boolean>(false);

    show(employeeId: number | undefined, id?: number): void {
        this.isNew.set(!id);
        this.detailsChanged.set(false);
        this.choosesEmployee.set(!employeeId);
        this.currentEmployee.set(undefined);
        this.balanceDueAmount.set(undefined);
        this.active.set(true);

        if (this.choosesEmployee()) {
            this.loadEmployees();
        }

        this._loanTypesService
            .getAll(
                new GetAllLoanTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.loanTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeLoanDto();
            record.employeeId = employeeId;
            record.actualAmount = 0;
            record.dueAmount = 0;
            record.fixedAmount = 0;
            // Legacy EmployeeLoan.AfterConstruction: all three dates start as today.
            const today = DateTime.now().startOf('day');
            record.startDate = today;
            record.recoveryStartDate = today;
            record.endDate = today;
            record.recoverFracInLastTrans = false;
            record.includeInLoanPay = true;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeLoanForEdit(id).subscribe((result) => {
                this.record.set(result.employeeLoan);
                this.balanceDueAmount.set(result.balanceDueAmount);
                this.currentEmployee.set(
                    new SimpleLookupDto({
                        id: result.employeeLoan.employeeId,
                        code: result.employeeCode,
                        nameEn: result.employeeFullNameEn,
                        nameAr: undefined,
                    })
                );
                this.employees.set(this.withCurrentEmployee(this.employees()));
                this.modal().show();
            });
        }
    }

    /** The loan's own employee stays selectable even if the lookup has not come back yet. */
    private withCurrentEmployee(items: SimpleLookupDto[]): SimpleLookupDto[] {
        const current = this.currentEmployee();
        if (!current || items.some((e) => e.id === current.id)) {
            return items;
        }

        return [current, ...items];
    }

    loadEmployees(): void {
        this._service
            .getEmployeesForLookup(new GetEmployeeLoanEmployeesInput({ filter: undefined }))
            .subscribe((result) => this.employees.set(this.withCurrentEmployee(result.items)));
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
                this.detailsChanged.set(false);
                this.close();
                this.modalSave.emit();
            });
    }

    /**
     * Saves a new loan so a tab can add a row under it, and keeps the dialog open on the saved loan. Undefined
     * when the form is incomplete (it says so) or the save fails (the server's message is already shown).
     */
    readonly saveForDetails = (): Promise<number | undefined> => {
        const form = this.recordForm();
        if (form && !form.valid) {
            form.control.markAllAsTouched();
            this.notify.warn(this.l('InvalidFormMessage'));
            return Promise.resolve(undefined);
        }

        this.saving.set(true);
        return new Promise((resolve) => {
            this._service
                .createOrEdit(this.record())
                .pipe(finalize(() => this.saving.set(false)))
                .subscribe({
                    next: (id) => {
                        this.notify.info(this.l('SavedSuccessfully'));
                        this.isNew.set(false);
                        this.updateField('id', id);
                        // The list behind has a new row, whatever happens next in this dialog.
                        this.detailsChanged.set(true);
                        resolve(id);
                    },
                    error: () => resolve(undefined),
                });
        });
    };

    /**
     * An instalment, recovery or exemption was saved: the server has recalculated the loan's recovery end date and
     * balance due, so those two are fetched again -- as legacy's form showed them change at once. The rest of the
     * form is left as the user has it.
     */
    onDetailsChanged(): void {
        this.detailsChanged.set(true);

        const id = this.record().id;
        if (!id) {
            return;
        }

        this._service.getEmployeeLoanForEdit(id).subscribe((result) => {
            this.updateField('endDate', result.employeeLoan.endDate);
            this.balanceDueAmount.set(result.balanceDueAmount);
        });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();

        // An instalment, recovery or exemption saved in the tabs is already stored even if the loan itself is
        // cancelled, and it can move the loan's balance, so the list behind still needs to refresh.
        if (this.detailsChanged()) {
            this.detailsChanged.set(false);
            this.modalSave.emit();
        }
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
