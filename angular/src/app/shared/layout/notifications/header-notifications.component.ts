import { Component, OnInit, ViewEncapsulation, inject, input, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NotificationServiceProxy, UserNotification } from '@shared/service-proxies/service-proxies';
import { IFormattedUserNotification, UserNotificationHelper } from './UserNotificationHelper';
import { forEach as _forEach } from 'lodash-es';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { NgTemplateOutlet, NgClass } from '@angular/common';
import { NgScrollbar } from 'ngx-scrollbar';
import { RouterLink } from '@angular/router';
import { LuxonFromNowPipe } from '../../../../shared/utils/luxon-from-now.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { Popover } from 'primeng/popover';

@Component({
    templateUrl: './header-notifications.component.html',
    selector: 'header-notifications',
    styleUrls: ['./header-notifications.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [Popover, NgTemplateOutlet, NgScrollbar, NgClass, RouterLink, LuxonFromNowPipe, LocalizePipe],
})
export class HeaderNotificationsComponent extends AppComponentBase implements OnInit {
    private _notificationService = inject(NotificationServiceProxy);
    private _userNotificationHelper = inject(UserNotificationHelper);

    readonly customStyle = input(
        'btn btn-active-color-primary btn-active-light btn-custom btn-icon btn-icon-muted h-35px h-md-40px position-relative w-35px w-md-40px'
    );
    readonly iconStyle = input('flaticon-alert-2 unread-notification fs-4');

    readonly notifications = signal<IFormattedUserNotification[]>([]);
    readonly unreadNotificationCount = signal<number>(0);

    ngOnInit(): void {
        this.loadNotifications();
        this.registerToEvents();
    }

    loadNotifications(): void {
        if (UrlHelper.isInstallUrl(location.href)) {
            return;
        }

        this._notificationService.getUserNotifications(undefined, undefined, undefined, 3, 0).subscribe((result) => {
            this.unreadNotificationCount.set(result.unreadCount);
            const formattedNotifications: IFormattedUserNotification[] = [];
            _forEach(result.items, (item: UserNotification) => {
                formattedNotifications.push(this._userNotificationHelper.format(<any>item));
            });
            this.notifications.set(formattedNotifications);
        });

        this.shouldUserUpdateApp();
    }

    registerToEvents() {
        const self = this;

        function onNotificationReceived(userNotification) {
            self._userNotificationHelper.show(userNotification);
            self.loadNotifications();
        }

        this.subscribeToEvent('abp.notifications.received', (userNotification) => {
            onNotificationReceived(userNotification);
        });

        function onNotificationsRefresh() {
            self.loadNotifications();
        }

        this.subscribeToEvent('app.notifications.refresh', () => {
            onNotificationsRefresh();
        });

        function onNotificationsRead(userNotificationId, success) {
            self.notifications.update((currentNotifications) => {
                const updated = currentNotifications.map((notification) => {
                    if (notification.userNotificationId === userNotificationId) {
                        return {
                            ...notification,
                            state: 'READ' as any,
                            isUnread: false,
                        };
                    }
                    return notification;
                });
                return updated;
            });

            if (success) {
                self.unreadNotificationCount.update((count) => Math.max(0, count - 1));
            }
        }

        this.subscribeToEvent('app.notifications.read', (userNotificationId, success) => {
            onNotificationsRead(userNotificationId, success);
        });
    }

    shouldUserUpdateApp(): void {
        this._userNotificationHelper.shouldUserUpdateApp();
    }

    setAllNotificationsAsRead(): void {
        this._userNotificationHelper.setAllAsRead();
    }

    openNotificationSettingsModal(): void {
        this._userNotificationHelper.openSettingsModal();
    }

    setNotificationAsRead(userNotification: IFormattedUserNotification): void {
        if (userNotification.state !== 'READ') {
            this._userNotificationHelper.setAsRead(userNotification.userNotificationId);
        }
    }

    gotoUrl(url): void {
        if (url) {
            location.href = url;
        }
    }
}
