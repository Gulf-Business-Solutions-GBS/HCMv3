import { Component, DestroyRef, computed, inject, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CostCenterDto,
    CostCentersServiceProxy,
    CreateOrEditEmployeePaymentDto,
    CurrenciesServiceProxy,
    CurrencyDto,
    EmployeePaymentsServiceProxy,
    GetAllCostCentersInput,
    GetAllCurrenciesInput,
    GetEmployeePaymentEmployeesInput,
    GetExpectedPaymentAmountInput,
    EmployeePaymentWageTypeLookupDto,
    SimpleLookupDto,
    UnitType,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Subject, of } from 'rxjs';
import { catchError, debounceTime, finalize, switchMap } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { SelectModule } from 'primeng/select';
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';

/**
 * Legacy (LHRMS) Payment transaction, laid out as the LHRMS screen: Employee / Wage Type / Payment Date /
 * Currency; Unit Type / Number / Billing Type; Cost Center / Expected Amount; then Comments and attachments.
 *
 * Billing Type is the wage type's, shown only when the payroll setting "Show Billing Type In Payment Screen"
 * is on. Expected Amount is shown only for Day / Hour and is worked out by the server whenever one of the five
 * fields it depends on changes (legacy ImmediatePostData).
 */
@Component({
    selector: 'createOrEditEmployeePaymentModal',
    templateUrl: './create-or-edit-employee-payment-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        SelectModule,
        AttachmentsComponent,
    ],
})
export class CreateOrEditEmployeePaymentModalComponent extends AppComponentBase {
    private _service = inject(EmployeePaymentsServiceProxy);
    private _currenciesService = inject(CurrenciesServiceProxy);
    private _costCentersService = inject(CostCentersServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeePaymentDto>(new CreateOrEditEmployeePaymentDto());

    readonly wageTypes = signal<EmployeePaymentWageTypeLookupDto[]>([]);
    readonly currencies = signal<CurrencyDto[]>([]);
    readonly costCenters = signal<CostCenterDto[]>([]);

    readonly showBillingType = signal<boolean>(false);
    readonly expectedAmount = signal<number>(0);

    /** Opened from the Payments list rather than an employee's record: the employee is chosen here. */
    readonly choosesEmployee = signal<boolean>(false);
    readonly employees = signal<SimpleLookupDto[]>([]);
    readonly currentEmployee = signal<SimpleLookupDto | undefined>(undefined);
    readonly employeeOptions = computed(() =>
        this.employees().map((e) => ({ id: e.id, label: `${e.code} - ${e.nameEn}` }))
    );

    /** "code - name" of the record's employee, for the read-only Employee field. */
    readonly employeeLabel = computed(
        () => this.employeeOptions().find((e) => e.id === this.record().employeeId)?.label ?? ''
    );

    /** The record's own wage type, kept selectable even when it is outside the lookup's criteria. */
    private readonly currentWageType = signal<EmployeePaymentWageTypeLookupDto | undefined>(undefined);

    readonly wageTypeOptions = computed(() => {
        const items = this.wageTypes();
        const current = this.currentWageType();
        return current && !items.some((w) => w.id === current.id) ? [current, ...items] : items;
    });

    /** Legacy non-persistent BillingType: the selected wage type's. */
    readonly billingType = computed(
        () => this.wageTypeOptions().find((w) => w.id === this.record().wageTypeId)?.billingType ?? ''
    );

    /** Legacy: Expected Amount is visible for Day and Hour only. */
    readonly showExpectedAmount = computed(
        () => this.record().unitType === UnitType.Day || this.record().unitType === UnitType.Hour
    );

    readonly unitTypes = [
        { value: UnitType.Amount, name: 'Amount' },
        { value: UnitType.Day, name: 'Day' },
        { value: UnitType.Hour, name: 'Hour' },
    ];

    private readonly recalculate$ = new Subject<CreateOrEditEmployeePaymentDto>();

    constructor() {
        super();

        this.recalculate$
            .pipe(
                debounceTime(250),
                switchMap((r) =>
                    this.canCalculate(r)
                        ? this._service
                              .getExpectedAmount(
                                  new GetExpectedPaymentAmountInput({
                                      employeeId: r.employeeId,
                                      wageTypeId: r.wageTypeId,
                                      paymentDate: r.paymentDate,
                                      unitType: r.unitType,
                                      amount: r.amount,
                                  })
                              )
                              .pipe(catchError(() => of(0)))
                        : of(0)
                ),
                takeUntilDestroyed(inject(DestroyRef))
            )
            .subscribe((amount) => this.expectedAmount.set(amount ?? 0));
    }

    show(employeeId: number | undefined, id?: number): void {
        this.choosesEmployee.set(!employeeId);
        this.currentEmployee.set(undefined);
        this.currentWageType.set(undefined);
        this.expectedAmount.set(0);
        this.active.set(true);

        this.loadLookups();

        // Also loaded when the employee is fixed (opened from the employee's record), to show its code and name.
        this.loadEmployees();

        if (!id) {
            this._service.getScreenSettings().subscribe((settings) => {
                this.showBillingType.set(settings.showBillingType);

                // Legacy EmployeePayment.AfterConstruction: today's date and the default currency.
                const record = new CreateOrEditEmployeePaymentDto();
                record.employeeId = employeeId;
                record.amount = 0;
                record.unitType = UnitType.Amount;
                record.paymentDate = DateTime.now().startOf('day');
                record.currencyId = settings.defaultCurrencyId;
                this.record.set(record);
                this.modal().show();
            });
        } else {
            this._service.getScreenSettings().subscribe((settings) => this.showBillingType.set(settings.showBillingType));
            this._service.getEmployeePaymentForEdit(id).subscribe((result) => {
                this.record.set(result.employeePayment);
                this.expectedAmount.set(result.expectedAmount ?? 0);
                this.currentEmployee.set(
                    new SimpleLookupDto({
                        id: result.employeePayment.employeeId,
                        code: result.employeeCode,
                        nameEn: result.employeeFullNameEn,
                        nameAr: undefined,
                    })
                );
                this.currentWageType.set(
                    new EmployeePaymentWageTypeLookupDto({
                        id: result.employeePayment.wageTypeId,
                        code: undefined,
                        nameEn: result.wageTypeNameEn,
                        billingType: result.billingType,
                    })
                );
                this.employees.set(this.withCurrentEmployee(this.employees()));
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._service.getWageTypesForLookup().subscribe((result) => this.wageTypes.set(result.items));

        this._currenciesService
            .getAll(
                new GetAllCurrenciesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.currencies.set(result.items));

        this._costCentersService
            .getAll(
                new GetAllCostCentersInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.costCenters.set(result.items));
    }

    private loadEmployees(): void {
        this._service
            .getEmployeesForLookup(new GetEmployeePaymentEmployeesInput({ filter: undefined }))
            .subscribe((result) => this.employees.set(this.withCurrentEmployee(result.items)));
    }

    private withCurrentEmployee(items: SimpleLookupDto[]): SimpleLookupDto[] {
        const current = this.currentEmployee();
        if (!current || items.some((e) => e.id === current.id)) {
            return items;
        }

        return [current, ...items];
    }

    private canCalculate(r: CreateOrEditEmployeePaymentDto): boolean {
        return (
            (r.unitType === UnitType.Day || r.unitType === UnitType.Hour) &&
            !!r.employeeId &&
            !!r.wageTypeId &&
            !!r.paymentDate &&
            !!r.amount
        );
    }

    onShown(): void {
        document.getElementById(this.choosesEmployee() ? 'EmployeePayment_EmployeeId' : 'EmployeePayment_WageTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeePaymentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeePaymentDto();
        Object.assign(next, current);
        this.record.set(next);

        if (['employeeId', 'wageTypeId', 'paymentDate', 'unitType', 'amount'].includes(field as string)) {
            this.recalculate$.next(next);
        }
    }

    updateDateField(field: keyof CreateOrEditEmployeePaymentDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeePaymentDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
