import { Component, inject, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NotificationDetailDto, NotificationServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { LuxonFormatPipe } from '../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { UserNotificationHelper } from './UserNotificationHelper';

@Component({
    selector: 'viewNotificationModal',
    templateUrl: './view-notification-modal.component.html',
    imports: [AppBsModalDirective, LuxonFormatPipe, LocalizePipe],
})
export class ViewNotificationModalComponent extends AppComponentBase {
    private _notificationService = inject(NotificationServiceProxy);
    private _userNotificationHelper = inject(UserNotificationHelper);

    readonly modal = viewChild<ModalDirective>('modal');

    notification = signal<NotificationDetailDto | undefined>(undefined);
    notificationId = signal<string>('');

    getSeverityClass = this._userNotificationHelper.getSeverityClass.bind(this._userNotificationHelper);
    getSeverityIcon = this._userNotificationHelper.getSeverityIcon.bind(this._userNotificationHelper);

    show(notificationId: string) {
        this.notificationId.set(notificationId);
        this.getNotification(notificationId, () => {
            this.modal().show();
        });
    }

    close(): void {
        this.modal().hide();
    }

    private getNotification(notificationId: string, callback: () => void) {
        this._notificationService.getNotificationDetail(notificationId).subscribe((result: NotificationDetailDto) => {
            this.notification.set(result);
            callback();
        });
    }
}
