import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { SelfServiceSettingsEditDto, ManageSelfServiceSettingsServiceProxy, RedirectAfterWFApproval } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManageSelfServiceSettingsModal',
    templateUrl: './create-or-edit-manage-self-service-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManageSelfServiceSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageSelfServiceSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<SelfServiceSettingsEditDto>(new SelfServiceSettingsEditDto());

    readonly redirectAfterWFApprovalOptions = [
        { value: RedirectAfterWFApproval.Default, name: 'Default' },
        { value: RedirectAfterWFApproval.ListView, name: 'ListView' },
        { value: RedirectAfterWFApproval.Dashboard, name: 'Dashboard' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new SelfServiceSettingsEditDto();
            record.redirectAfterWFApproval = RedirectAfterWFApproval.Default;
            record.allowPunchOutsideRange = false;
            record.autoLogoutAfterPunch = false;
            record.allowGeolocationSelfie = false;
            record.hideGeolocationInfo = false;
            record.hideGeolocationPunchInfo = false;
            record.emailNotification = false;
            record.defaultLoginPage = undefined;
            record.captchaKey = undefined;
            record.showLeaveInfo = false;
            record.showCurrentLeaveBalance = false;
            record.showUpdatedLeaveBalance = false;
            record.showEmployeeInfo = false;
            record.isAttachmentAllowed = false;
            record.isAdditionalUnpaidLeaveAllowed = false;
            record.showPaymentDate = false;
            record.enablePayWithPayroll = false;
            record.enablePayInAdvance = false;
            record.isPassportWithdrawalRequired = false;
            record.passportWithdrawalSubmissionStep = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getSelfServiceSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SelfServiceSettings_RedirectAfterWFApproval')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditSelfServiceSettings(this.record())
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

    updateField(field: keyof SelfServiceSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new SelfServiceSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateNumberField(field: keyof SelfServiceSettingsEditDto, value: string): void {
        this.updateField(field, value !== null && value !== undefined && value !== '' ? Number(value) : 0);
    }
}
