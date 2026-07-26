import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditEmailNotificationLogDto, EmailNotificationLogsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmailNotificationLogModal',
    templateUrl: './create-or-edit-email-notification-log-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmailNotificationLogModalComponent extends AppComponentBase {
    private _service = inject(EmailNotificationLogsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmailNotificationLogDto>(new CreateOrEditEmailNotificationLogDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditEmailNotificationLogDto();
            record.isSuccessful = false;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmailNotificationLogForEdit(id).subscribe((result) => {
                this.record.set(result.emailNotificationLog);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmailNotificationLog_Email')?.focus();
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

    updateField(field: keyof CreateOrEditEmailNotificationLogDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmailNotificationLogDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    toDateTimeLocal(value?: DateTime): string {
        return value ? value.toFormat("yyyy-MM-dd'T'HH:mm") : '';
    }

    updateDateTimeField(field: keyof CreateOrEditEmailNotificationLogDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
