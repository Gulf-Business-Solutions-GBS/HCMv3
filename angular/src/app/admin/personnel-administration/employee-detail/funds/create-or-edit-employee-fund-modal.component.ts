import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeFundDto,
    EmployeeFundsServiceProxy,
    GetEmployeeFundEmployeesInput,
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
import { SelectModule } from 'primeng/select';
import { EmployeeFundTabsComponent } from './employee-fund-tabs.component';

/** The fund's read-only totals, worked out on the server from its Fund Detail rows. */
interface FundTotals {
    amount: number;
    paidAmount: number;
    balanceAmount: number;
}

/**
 * The legacy (LHRMS) Fund form: Employee, Start Date, End Date; the read-only Amount, Paid Amount and Balance
 * Amount; Comments; then the Fund Detail and Fund Exemption tabs. Used from the Fund list (employee chosen here)
 * and from an employee's Funds tab (employee fixed).
 */
@Component({
    selector: 'createOrEditEmployeeFundModal',
    templateUrl: './create-or-edit-employee-fund-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        SelectModule,
        EmployeeFundTabsComponent,
    ],
})
export class CreateOrEditEmployeeFundModalComponent extends AppComponentBase {
    private _service = inject(EmployeeFundsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly recordForm = viewChild<NgForm>('recordForm');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeFundDto>(new CreateOrEditEmployeeFundDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    /** True when opened with no employee given (from the Fund list): the employee is chosen here. */
    readonly choosesEmployee = signal<boolean>(false);

    readonly employees = signal<SimpleLookupDto[]>([]);

    /** The dropdown's entries, "code - name", searchable inside the dropdown itself. */
    readonly employeeOptions = computed(() =>
        this.employees().map((e) => ({ id: e.id, label: `${e.code} - ${e.nameEn}` }))
    );

    /** The fund's employee as loaded for edit; always offered in the dropdown, and shown when it is read-only. */
    readonly currentEmployee = signal<SimpleLookupDto | undefined>(undefined);

    /** Read-only on the form, as in legacy. */
    readonly totals = signal<FundTotals>({ amount: 0, paidAmount: 0, balanceAmount: 0 });

    /** Set when something was saved in the tabs under the fund. */
    readonly detailsChanged = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'EmployeeFund',
            record: this.record,
            isNew: this.isNew,
            keys: () => ({ EmployeeId: this.record().employeeId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    show(employeeId: number | undefined, id?: number): void {
        this.isNew.set(!id);
        this.detailsChanged.set(false);
        this.choosesEmployee.set(!employeeId);
        this.currentEmployee.set(undefined);
        this.totals.set({ amount: 0, paidAmount: 0, balanceAmount: 0 });
        this.active.set(true);

        if (this.choosesEmployee()) {
            this.loadEmployees();
        }

        if (!id) {
            const record = new CreateOrEditEmployeeFundDto();
            record.employeeId = employeeId;
            // Legacy EmployeeFund.AfterConstruction: both dates start as today.
            const today = DateTime.now().startOf('day');
            record.startDate = today;
            record.endDate = today;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeFundForEdit(id).subscribe((result) => {
                this.record.set(result.employeeFund);
                this.setTotals(result);
                this.currentEmployee.set(
                    new SimpleLookupDto({
                        id: result.employeeFund.employeeId,
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

    private setTotals(result: { amount: number; paidAmount: number; balanceAmount: number }): void {
        this.totals.set({
            amount: result.amount ?? 0,
            paidAmount: result.paidAmount ?? 0,
            balanceAmount: result.balanceAmount ?? 0,
        });
    }

    /** The fund's own employee stays selectable even if the lookup has not come back yet. */
    private withCurrentEmployee(items: SimpleLookupDto[]): SimpleLookupDto[] {
        const current = this.currentEmployee();
        if (!current || items.some((e) => e.id === current.id)) {
            return items;
        }

        return [current, ...items];
    }

    loadEmployees(): void {
        this._service
            .getEmployeesForLookup(new GetEmployeeFundEmployeesInput({ filter: undefined }))
            .subscribe((result) => this.employees.set(this.withCurrentEmployee(result.items)));
    }

    onShown(): void {
        document
            .getElementById(this.choosesEmployee() ? 'EmployeeFund_EmployeeId' : 'EmployeeFund_StartDate')
            ?.focus();
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
     * Saves a new fund so a tab can add a row under it, and keeps the dialog open on the saved fund. Undefined
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
     * A fund detail or exemption was saved: the server has re-totalled the fund, so the three totals are fetched
     * again -- as legacy's form showed them change at once. The rest of the form is left as the user has it.
     */
    onDetailsChanged(): void {
        this.detailsChanged.set(true);

        const id = this.record().id;
        if (!id) {
            return;
        }

        this._service.getEmployeeFundForEdit(id).subscribe((result) => this.setTotals(result));
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();

        // Rows saved in the tabs are already stored even if the fund itself is cancelled, and they move the
        // fund's totals, so the list behind still needs to refresh.
        if (this.detailsChanged()) {
            this.detailsChanged.set(false);
            this.modalSave.emit();
        }
    }

    updateField(field: keyof CreateOrEditEmployeeFundDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeFundDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeFundDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
