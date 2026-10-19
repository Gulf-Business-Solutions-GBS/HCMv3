import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    CreateOrEditEmployeeAbsQuotaForfeitDto,
    EmployeeAbsQuotaForfeitsServiceProxy,
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
    selector: 'createOrEditEmployeeAbsQuotaForfeitModal',
    templateUrl: './create-or-edit-employee-abs-quota-forfeit-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeAbsQuotaForfeitModalComponent extends AppComponentBase {
    private _service = inject(EmployeeAbsQuotaForfeitsServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeAbsQuotaForfeitDto>(new CreateOrEditEmployeeAbsQuotaForfeitDto());

    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeAbsQuotaForfeitDto();
            record.employeeId = employeeId;
            record.quotaForfeited = 0;
            record.dontUpdateQuotaBalance = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeAbsQuotaForfeitForEdit(id).subscribe((result) => {
                this.record.set(result.employeeAbsQuotaForfeit);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeAbsQuotaForfeit_AbsenceTypeId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeAbsQuotaForfeitDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeAbsQuotaForfeitDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeAbsQuotaForfeitDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeAbsQuotaForfeitDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
