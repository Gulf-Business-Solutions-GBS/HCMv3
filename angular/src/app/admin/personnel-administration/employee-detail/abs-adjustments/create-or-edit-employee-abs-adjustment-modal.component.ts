import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    AdjustmentPaymentType,
    AdjustmentType,
    CreateOrEditEmployeeAbsAdjustmentDto,
    EmployeeAbsAdjustmentsServiceProxy,
    GetAllAbsenceTypesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeAbsAdjustmentModal',
    templateUrl: './create-or-edit-employee-abs-adjustment-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeAbsAdjustmentModalComponent extends AppComponentBase {
    private _service = inject(EmployeeAbsAdjustmentsServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeAbsAdjustmentDto>(new CreateOrEditEmployeeAbsAdjustmentDto());

    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

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

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeAbsAdjustmentDto();
            record.employeeId = employeeId;
            record.adjustmentType = AdjustmentType.Adjust;
            record.adjustmentPaymentType = AdjustmentPaymentType.PayWithPayroll;
            record.quotaAdjusted = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeAbsAdjustmentForEdit(id).subscribe((result) => {
                this.record.set(result.employeeAbsAdjustment);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeAbsAdjustment_AbsenceTypeId')?.focus();
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
