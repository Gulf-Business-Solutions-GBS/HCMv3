import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    CreateOrEditEmployeeAbsenceQuotaDto,
    EmployeeAbsenceQuotasServiceProxy,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditEmployeeAbsenceQuotaModal',
    templateUrl: './create-or-edit-employee-absence-quota-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeAbsenceQuotaModalComponent extends AppComponentBase {
    private _service = inject(EmployeeAbsenceQuotasServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeAbsenceQuotaDto>(new CreateOrEditEmployeeAbsenceQuotaDto());

    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    /**
     * Read-only balance, shown on edit. The edit DTO does not carry it, so the list passes in the value
     * from its row (EmployeeAbsenceQuotaDto); it is as of the last save, not recalculated here.
     */
    readonly quotaBalance = signal<number | undefined>(undefined);

    show(employeeId: number, id?: number, quotaBalance?: number): void {
        this.active.set(true);
        this.quotaBalance.set(quotaBalance);

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

        if (!id) {
            const record = new CreateOrEditEmployeeAbsenceQuotaDto();
            record.employeeId = employeeId;
            record.quotaAccrued = 0;
            record.quotaUsed = 0;
            record.quotaAdjusted = 0;
            record.quotaForfeited = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeAbsenceQuotaForEdit(id).subscribe((result) => {
                this.record.set(result.employeeAbsenceQuota);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeAbsenceQuota_AbsenceTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeAbsenceQuotaDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeAbsenceQuotaDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeAbsenceQuotaDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeAbsenceQuotaDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
