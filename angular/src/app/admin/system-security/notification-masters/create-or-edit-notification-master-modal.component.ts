import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditNotificationMasterDto,
    GetAllNotificationConfigsInput,
    NotificationConfigDto,
    NotificationConfigsServiceProxy,
    NotificationMastersServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditNotificationMasterModal',
    templateUrl: './create-or-edit-notification-master-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditNotificationMasterModalComponent extends AppComponentBase {
    private _service = inject(NotificationMastersServiceProxy);
    private _notificationConfigsService = inject(NotificationConfigsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditNotificationMasterDto>(new CreateOrEditNotificationMasterDto());
    readonly notificationConfigs = signal<NotificationConfigDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._notificationConfigsService
            .getAll(new GetAllNotificationConfigsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.notificationConfigs.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditNotificationMasterDto());
            this.modal().show();
        } else {
            this._service.getNotificationMasterForEdit(id).subscribe((result) => {
                this.record.set(result.notificationMaster);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('NotificationMaster_NotificationType')?.focus();
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

    updateField(field: keyof CreateOrEditNotificationMasterDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditNotificationMasterDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
