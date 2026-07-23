import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPayrollCalendarDto,
    GetAllPayrollAreasInput,
    PayrollAreaDto,
    PayrollAreasServiceProxy,
    PayrollCalendarsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditPayrollCalendarModal',
    templateUrl: './create-or-edit-payroll-calendar-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPayrollCalendarModalComponent extends AppComponentBase {
    private _payrollCalendarsService = inject(PayrollCalendarsServiceProxy);
    private _payrollAreasService = inject(PayrollAreasServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly payrollCalendar = signal<CreateOrEditPayrollCalendarDto>(new CreateOrEditPayrollCalendarDto());
    readonly payrollAreas = signal<PayrollAreaDto[]>([]);

    show(payrollCalendarId?: number): void {
        this.active.set(true);

        this._payrollAreasService
            .getAll(new GetAllPayrollAreasInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.payrollAreas.set(result.items));

        if (!payrollCalendarId) {
            this.payrollCalendar.set(new CreateOrEditPayrollCalendarDto());
            this.modal().show();
        } else {
            this._payrollCalendarsService.getPayrollCalendarForEdit(payrollCalendarId).subscribe((result) => {
                this.payrollCalendar.set(result.payrollCalendar);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PayrollCalendar_PayrollAreaId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._payrollCalendarsService
            .createOrEdit(this.payrollCalendar())
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

    updateField(field: keyof CreateOrEditPayrollCalendarDto, value: any): void {
        const current = this.payrollCalendar();
        (current as any)[field] = value;
        const next = new CreateOrEditPayrollCalendarDto();
        Object.assign(next, current);
        this.payrollCalendar.set(next);
    }

    updateDateField(field: keyof CreateOrEditPayrollCalendarDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
