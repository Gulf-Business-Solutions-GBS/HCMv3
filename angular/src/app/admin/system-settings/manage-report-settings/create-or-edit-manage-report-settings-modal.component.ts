import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ReportSettingsEditDto, ManageReportSettingsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditManageReportSettingsModal',
    templateUrl: './create-or-edit-manage-report-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManageReportSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageReportSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<ReportSettingsEditDto>(new ReportSettingsEditDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new ReportSettingsEditDto();
            record.showSubordinatesPayslip = false;
            record.exportGridReportWithHeader = false;
            record.exportGridReportWithFooter = false;
            record.showPayslipOnDay = 0;
            record.decimalInReports = 0;
            record.showPeriodInPayrollSheet = false;
            record.showPeriodInLeaveProvision = false;
            record.showPeriodInIndemnityProvision = false;
            record.periodMonthLimitInPayrollSheet = 0;
            record.periodMonthLimitInLeaveProvision = 0;
            record.periodMonthLimitInIndemnityProvision = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getReportSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ReportSettings_ShowPayslipOnDay')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditReportSettings(this.record())
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

    updateField(field: keyof ReportSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new ReportSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof ReportSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }

    updateDateField(field: keyof ReportSettingsEditDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
