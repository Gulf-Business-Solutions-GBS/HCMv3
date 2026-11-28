import { Component, DestroyRef, computed, inject, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeOvertimeDto,
    EmployeeOvertimesServiceProxy,
    GetAllOvertimeTypesInput,
    GetEmployeeOvertimeEmployeesInput,
    GetExpectedOvertimeAmountInput,
    OvertimeTypeDto,
    OvertimeTypesServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { SelectModule } from 'primeng/select';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { Observable, Subject, of } from 'rxjs';
import { catchError, debounceTime, finalize, switchMap } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { RecordDirectlyCheckComponent } from '@app/shared/common/record-directly/record-directly-check.component';

@Component({
    selector: 'createOrEditEmployeeOvertimeModal',
    templateUrl: './create-or-edit-employee-overtime-modal.component.html',
    imports: [
        RecordDirectlyCheckComponent,
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        DecimalPipe,
        SelectModule,
    ],
})
export class CreateOrEditEmployeeOvertimeModalComponent extends AppComponentBase {
    private _service = inject(EmployeeOvertimesServiceProxy);
    private _overtimeTypesService = inject(OvertimeTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeOvertimeDto>(new CreateOrEditEmployeeOvertimeDto());

    readonly overtimeTypes = signal<OvertimeTypeDto[]>([]);

    /**
     * Read-only (legacy non-persistent ExpectedOvertimeAmount). Worked out by the server from what the form holds
     * and refreshed whenever Employee, Effective Date, Overtime Type or Approved Hours change, as legacy's
     * ImmediatePostData did. Never posted back.
     */
    readonly expectedOvertimeAmount = signal<number>(0);

    private readonly _destroyRef = inject(DestroyRef);
    private readonly _amountRequests = new Subject<CreateOrEditEmployeeOvertimeDto>();

    /** The fields legacy marked ImmediatePostData: changing any of them recalculates the amount. */
    private static readonly amountInputs: (keyof CreateOrEditEmployeeOvertimeDto)[] = [
        'employeeId',
        'effectiveDate',
        'overtimeTypeId',
        'approvedOvertimeHours',
    ];

    constructor() {
        super();

        // Debounced so typing hours does not fire one request per keystroke; switchMap drops a stale answer.
        this._amountRequests
            .pipe(
                debounceTime(300),
                switchMap((record) => this.fetchExpectedAmount(record)),
                takeUntilDestroyed(this._destroyRef)
            )
            .subscribe((amount) => this.expectedOvertimeAmount.set(amount));
    }

    /**
     * True when opened from the Employee Overtime list (no employee given): the employee is chosen here, as on
     * legacy's own Overtime screen. False inside an employee's record, where the employee is fixed.
     */
    readonly choosesEmployee = signal<boolean>(false);

    readonly employees = signal<SimpleLookupDto[]>([]);

    /** The dropdown's entries, "code - name", searchable inside the dropdown itself. */
    readonly employeeOptions = computed(() =>
        this.employees().map((e) => ({ id: e.id, label: `${e.code} - ${e.nameEn}` }))
    );

    /** The record's employee as loaded for edit; always offered in the dropdown. */
    readonly currentEmployee = signal<SimpleLookupDto | undefined>(undefined);

    show(employeeId: number | undefined, id?: number): void {
        this.choosesEmployee.set(!employeeId);
        this.currentEmployee.set(undefined);
        this.active.set(true);

        if (this.choosesEmployee()) {
            this._service
                .getEmployeesForLookup(new GetEmployeeOvertimeEmployeesInput({ filter: undefined }))
                .subscribe((result) => this.employees.set(this.withCurrentEmployee(result.items)));
        }

        this._overtimeTypesService
            .getAll(
                new GetAllOvertimeTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.overtimeTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeOvertimeDto();
            record.employeeId = employeeId;
            // Legacy AfterConstruction: EffectiveDate = today.
            record.effectiveDate = DateTime.now().startOf('day');
            record.overtimeHours = 0;
            record.actualOvertimeHours = 0;
            record.approvedOvertimeHours = 0;
            this.record.set(record);
            this.expectedOvertimeAmount.set(0);
            this.modal().show();
        } else {
            this._service.getEmployeeOvertimeForEdit(id).subscribe((result) => {
                this.record.set(result.employeeOvertime);
                this.expectedOvertimeAmount.set(result.expectedOvertimeAmount ?? 0);
                this.currentEmployee.set(
                    new SimpleLookupDto({
                        id: result.employeeOvertime.employeeId,
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

    /** The record's own employee stays selectable even if the lookup has not come back yet. */
    private withCurrentEmployee(items: SimpleLookupDto[]): SimpleLookupDto[] {
        const current = this.currentEmployee();
        if (!current || items.some((e) => e.id === current.id)) {
            return items;
        }
        return [current, ...items];
    }

    onShown(): void {
        document.getElementById('EmployeeOvertime_OvertimeTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeOvertimeDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeOvertimeDto();
        Object.assign(next, current);
        this.record.set(next);

        if (CreateOrEditEmployeeOvertimeModalComponent.amountInputs.includes(field)) {
            this._amountRequests.next(next);
        }
    }

    private fetchExpectedAmount(record: CreateOrEditEmployeeOvertimeDto): Observable<number> {
        // Legacy getter gate: no amount until employee, type, date and non-zero approved hours are all there.
        if (!record.employeeId || !record.overtimeTypeId || !record.effectiveDate || !record.approvedOvertimeHours) {
            return of(0);
        }

        const input = new GetExpectedOvertimeAmountInput();
        input.employeeId = record.employeeId;
        input.overtimeTypeId = record.overtimeTypeId;
        input.effectiveDate = record.effectiveDate;
        input.approvedOvertimeHours = record.approvedOvertimeHours;
        return this._service.getExpectedOvertimeAmount(input).pipe(catchError(() => of(0)));
    }

    updateDateField(field: keyof CreateOrEditEmployeeOvertimeDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeOvertimeDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
