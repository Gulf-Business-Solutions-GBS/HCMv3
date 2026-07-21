import { Component, inject, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    GetNotificationSettingsOutput,
    NotificationServiceProxy,
    NotificationSubscriptionDto,
    UpdateNotificationSettingsInput,
} from '@shared/service-proxies/service-proxies';
import { map as _map } from 'lodash-es';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';

import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'notificationSettingsModal',
    templateUrl: './notification-settings-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class NotificationSettingsModalComponent extends AppComponentBase {
    private _notificationService = inject(NotificationServiceProxy);

    readonly modal = viewChild<ModalDirective>('modal');

    saving = signal(false);

    settings = signal<GetNotificationSettingsOutput | undefined>(undefined);

    show() {
        this.getSettings(() => {
            this.modal().show();
        });
    }

    save(): void {
        const settingsValue = this.settings();
        const input = new UpdateNotificationSettingsInput();
        input.receiveNotifications = settingsValue.receiveNotifications;
        input.notifications = _map(settingsValue.notifications, (n) => {
            let subscription = new NotificationSubscriptionDto();
            subscription.name = n.name;
            subscription.isSubscribed = n.isSubscribed;
            return subscription;
        });

        this.saving.set(true);
        this._notificationService
            .updateNotificationSettings(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
            });
    }

    close(): void {
        this.modal().hide();
    }

    private getSettings(callback: () => void) {
        this._notificationService.getNotificationSettings().subscribe((result: GetNotificationSettingsOutput) => {
            this.settings.set(result);
            callback();
        });
    }
}
