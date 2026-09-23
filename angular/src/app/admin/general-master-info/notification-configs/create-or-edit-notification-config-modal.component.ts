import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    NotificationConfigsServiceProxy,
    CreateOrEditNotificationConfigDto,
    EmailTemplateDto,
    EmailTemplatesServiceProxy,
    GetAllEmailTemplatesInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditNotificationConfigModal',
    templateUrl: './create-or-edit-notification-config-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditNotificationConfigModalComponent extends AppComponentBase {
    private _notificationConfigService = inject(NotificationConfigsServiceProxy);
    private _emailTemplatesService = inject(EmailTemplatesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly notificationConfig = signal<CreateOrEditNotificationConfigDto>(new CreateOrEditNotificationConfigDto());
    readonly emailTemplates = signal<EmailTemplateDto[]>([]);

    show(notificationConfigId?: number): void {
        this.active.set(true);

        this._emailTemplatesService
            .getAll(new GetAllEmailTemplatesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.emailTemplates.set(result.items));

        if (!notificationConfigId) {
            this.notificationConfig.set(new CreateOrEditNotificationConfigDto());
            this.modal().show();
        } else {
            this._notificationConfigService.getNotificationConfigForEdit(notificationConfigId).subscribe((result) => {
                this.notificationConfig.set(result.notificationConfig);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('NotificationConfig_Name')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._notificationConfigService
            .createOrEdit(this.notificationConfig())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditNotificationConfigDto, value: any): void {
        const current = this.notificationConfig();
        (current as any)[field] = value;
        const next = new CreateOrEditNotificationConfigDto();
        Object.assign(next, current);
        this.notificationConfig.set(next);
    }
}
