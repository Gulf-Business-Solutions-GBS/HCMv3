import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditPayrollLogDto, PayrollLogsServiceProxy } from '@shared/service-proxies/service-proxies';
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
    selector: 'createOrEditPayrollLogModal',
    templateUrl: './create-or-edit-payroll-log-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditPayrollLogModalComponent extends AppComponentBase {
    private _payrollLogsService = inject(PayrollLogsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly payrollLog = signal<CreateOrEditPayrollLogDto>(new CreateOrEditPayrollLogDto());

    show(payrollLogId?: number): void {
        this.active.set(true);

        if (!payrollLogId) {
            this.payrollLog.set(new CreateOrEditPayrollLogDto());
            this.modal().show();
        } else {
            this._payrollLogsService.getPayrollLogForEdit(payrollLogId).subscribe((result) => {
                this.payrollLog.set(result.payrollLog);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PayrollLog_Message')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._payrollLogsService
            .createOrEdit(this.payrollLog())
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

    updateField(field: keyof CreateOrEditPayrollLogDto, value: any): void {
        const current = this.payrollLog();
        (current as any)[field] = value;
        const next = new CreateOrEditPayrollLogDto();
        Object.assign(next, current);
        this.payrollLog.set(next);
    }

    updateDateField(field: keyof CreateOrEditPayrollLogDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
