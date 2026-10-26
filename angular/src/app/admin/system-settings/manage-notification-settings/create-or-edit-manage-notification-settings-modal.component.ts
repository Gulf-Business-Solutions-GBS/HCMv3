import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    NotificationSettingsEditDto,
    ManageNotificationSettingsServiceProxy,
    AdvancePaymentReminderDateType,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditManageNotificationSettingsModal',
    templateUrl: './create-or-edit-manage-notification-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditManageNotificationSettingsModalComponent extends AppComponentBase {
    private _service = inject(ManageNotificationSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<NotificationSettingsEditDto>(new NotificationSettingsEditDto());

    readonly reminderDateTypes = [
        { value: AdvancePaymentReminderDateType.LeaveStartDate, name: 'LeaveStartDate' },
        { value: AdvancePaymentReminderDateType.LeavePaymentDate, name: 'LeavePaymentDate' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new NotificationSettingsEditDto();
            record.disableNotificationWindow = false;
            record.advancePaymentReminderDateType = AdvancePaymentReminderDateType.LeaveStartDate;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getNotificationSettingsForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('NotificationSettings_AdvancePaymentReminderDateType')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditNotificationSettings(this.record())
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

    updateField(field: keyof NotificationSettingsEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new NotificationSettingsEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
