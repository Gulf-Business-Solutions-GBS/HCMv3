import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeDependantExceptionDto,
    EmployeeDependantDto,
    EmployeeDependantExceptionsServiceProxy,
    EmployeeDependantsServiceProxy,
    ExceptionBenefitType,
    GetAllEmployeeDependantsInput,
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
    selector: 'createOrEditEmployeeDependantExceptionModal',
    templateUrl: './create-or-edit-employee-dependant-exception-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeDependantExceptionModalComponent extends AppComponentBase {
    private _service = inject(EmployeeDependantExceptionsServiceProxy);
    private _employeeDependantsService = inject(EmployeeDependantsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeDependantExceptionDto>(new CreateOrEditEmployeeDependantExceptionDto());

    readonly employeeDependants = signal<EmployeeDependantDto[]>([]);

    readonly exceptionBenefitTypes = [
        { value: ExceptionBenefitType.Ticket, name: 'Ticket' },
        { value: ExceptionBenefitType.HealthReimbursment, name: 'HealthReimbursment' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._employeeDependantsService
            .getAll(new GetAllEmployeeDependantsInput({ employeeId: undefined, maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.employeeDependants.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeDependantExceptionDto();
            record.exceptionBenefitType = ExceptionBenefitType.Ticket;
            record.amount = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeDependantExceptionForEdit(id).subscribe((result) => {
                this.record.set(result.employeeDependantException);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeDependantException_EmployeeDependantId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeDependantExceptionDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeDependantExceptionDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeDependantExceptionDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeDependantExceptionDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
