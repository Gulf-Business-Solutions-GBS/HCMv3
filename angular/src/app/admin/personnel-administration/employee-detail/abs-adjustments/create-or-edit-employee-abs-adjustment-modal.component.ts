import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    AdjustmentPaymentType,
    AdjustmentType,
    CreateOrEditEmployeeAbsAdjustmentDto,
    EmployeeAbsAdjustmentsServiceProxy,
    EmployeesServiceProxy,
    GetAllAbsenceTypesInput,
    GetAllEmployeesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

interface EmployeeOption {
    id: number;
    label: string;
}

/**
 * Legacy (LHRMS) "Leave Abs Adjustment" (EmployeeAbsAdjustment_DetailView), laid out as the LHRMS screen:
 * Employee / Effective Date / Absence Type, then Adjustment Type / Quota Adjusted / Adjustment Payment Type,
 * then Comments. The annual-quota calculation runs on the server when the record is saved or deleted.
 */
@Component({
    selector: 'createOrEditEmployeeAbsAdjustmentModal',
    templateUrl: './create-or-edit-employee-abs-adjustment-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        SelectModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeAbsAdjustmentModalComponent extends AppComponentBase {
    private _service = inject(EmployeeAbsAdjustmentsServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeAbsAdjustmentDto>(new CreateOrEditEmployeeAbsAdjustmentDto());

    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    /** Opened from the all-employees list rather than an employee's record: the employee is chosen here. */
    readonly choosesEmployee = signal<boolean>(false);
    readonly employees = signal<EmployeeOption[]>([]);
    /** "code name" of the record's employee as the server gave it (edit), kept if not in the loaded list. */
    readonly currentEmployee = signal<EmployeeOption | undefined>(undefined);

    readonly employeeOptions = computed(() => {
        const list = this.employees();
        const current = this.currentEmployee();
        return current && !list.some((e) => e.id === current.id) ? [current, ...list] : list;
    });

    readonly employeeLabel = computed(
        () => this.employeeOptions().find((e) => e.id === this.record().employeeId)?.label ?? ''
    );

    readonly adjustmentTypes = [
        { value: AdjustmentType.Encashment, name: 'Encashment' },
        { value: AdjustmentType.Adjust, name: 'Adjust' },
        { value: AdjustmentType.Fractional, name: 'Fractional' },
        { value: AdjustmentType.NoImpactOnQuota, name: 'NoImpactOnQuota' },
    ];

    readonly adjustmentPaymentTypes = [
        { value: AdjustmentPaymentType.PayWithPayroll, name: 'PayWithPayroll' },
        { value: AdjustmentPaymentType.PayInAdvance, name: 'PayInAdvance' },
    ];

    show(employeeId: number | undefined, id?: number): void {
        this.active.set(true);
        this.choosesEmployee.set(!employeeId);
        this.currentEmployee.set(undefined);

        this._absenceTypesService
            .getAll(
                new GetAllAbsenceTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.absenceTypes.set(result.items));

        // Also loaded when the employee is fixed, to show its code and name in the read-only field.
        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) =>
                this.employees.set(result.items.map((e) => ({ id: e.id, label: `${e.code} ${e.fullNameEn ?? ''}`.trim() })))
            );

        if (!id) {
            // Legacy AfterConstruction: EffectiveDate = today. Enum defaults are the first members.
            const record = new CreateOrEditEmployeeAbsAdjustmentDto();
            record.employeeId = employeeId;
            record.effectiveDate = DateTime.local().startOf('day');
            record.adjustmentType = AdjustmentType.Encashment;
            record.adjustmentPaymentType = AdjustmentPaymentType.PayWithPayroll;
            record.quotaAdjusted = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeAbsAdjustmentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeAbsAdjustment);
                this.currentEmployee.set({
                    id: result.employeeAbsAdjustment.employeeId,
                    label: result.employeeCodeWithFullNameEn,
                });
                this.modal().show();
            });
        }
    }

    onShown(): void {
        const focusId = this.choosesEmployee() ? 'EmployeeAbsAdjustment_EmployeeId' : 'EmployeeAbsAdjustment_EffectiveDate';
        document.getElementById(focusId)?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeAbsAdjustmentDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeAbsAdjustmentDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeAbsAdjustmentDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeAbsAdjustmentDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
