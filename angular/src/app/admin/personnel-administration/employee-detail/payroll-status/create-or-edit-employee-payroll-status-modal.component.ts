import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditEmployeePayrollStatusDto, EmployeePayrollStatusesServiceProxy } from '@shared/service-proxies/service-proxies';
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
    selector: 'createOrEditEmployeePayrollStatusModal',
    templateUrl: './create-or-edit-employee-payroll-status-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeePayrollStatusModalComponent extends AppComponentBase {
    private _service = inject(EmployeePayrollStatusesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeePayrollStatusDto>(new CreateOrEditEmployeePayrollStatusDto());

    readonly dateFields: (keyof CreateOrEditEmployeePayrollStatusDto)[] = [
        'earliestRetroAccountingDate',
        'accountedTo',
        'runPayrollUpto',
        'earliestMasterDataChange',
        'doNoAccountAfter',
        'earliestRecalculationDate',
        'personalCalendarFrom',
        'nextTimeEvaluationFrom',
    ];

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditEmployeePayrollStatusDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeePayrollStatusForEdit(id).subscribe((result) => {
                this.record.set(result.employeePayrollStatus);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeePayrollStatus_AccountedTo')?.focus();
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

    getDateValue(field: keyof CreateOrEditEmployeePayrollStatusDto): string | undefined {
        const value = (this.record() as any)[field] as DateTime | undefined;
        return value?.toISODate() ?? undefined;
    }

    updateField(field: keyof CreateOrEditEmployeePayrollStatusDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeePayrollStatusDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeePayrollStatusDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    fieldLabel(field: string): string {
        return field.charAt(0).toUpperCase() + field.slice(1);
    }
}
