import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditEmployeeTimesheetDto, EmployeeTimesheetsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeTimesheetModal',
    templateUrl: './create-or-edit-employee-timesheet-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeTimesheetModalComponent extends AppComponentBase {
    private _service = inject(EmployeeTimesheetsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeTimesheetDto>(new CreateOrEditEmployeeTimesheetDto());

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditEmployeeTimesheetDto();
            record.employeeId = employeeId;
            record.totalHours = 0;
            record.approved = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeTimesheetForEdit(id).subscribe((result) => {
                this.record.set(result.employeeTimesheet);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeTimesheet_TransDate')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeTimesheetDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeTimesheetDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateTransDate(value: string): void {
        this.updateField('transDate', value ? DateTime.fromISO(value) : (undefined as any));
    }

    updateNumberField(field: keyof CreateOrEditEmployeeTimesheetDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : (undefined as any));
    }
}
