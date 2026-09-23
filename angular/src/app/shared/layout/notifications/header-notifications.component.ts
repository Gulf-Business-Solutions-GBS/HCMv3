import { Component, DestroyRef, OnInit, ViewEncapsulation, inject, input, signal } from '@angular/core';
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
    private _destroyRef = inject(DestroyRef);

    /**
     * How often the unread count is re-read from the server while somebody sits on a page.
     *
     * A BACKSTOP, NOT THE MECHANISM. The count is meant to move the instant a notification arrives over
     * SignalR, and it still does. But the badge was reported reading zero while a notification plainly
     * existed, and every way that happens has the same shape: the count was fetched once at load and
     * nothing refetched it -- the hub connection dropped and silently did not come back, the
     * notification was raised before the connection was up, or it was created in another tab. A minute
     * is short enough that nobody notices the staleness and long enough to be free.
     */
    private static readonly RefreshIntervalMs = 60000;

    readonly customStyle = input(
        'btn btn-active-color-primary btn-active-light btn-custom btn-icon btn-icon-muted h-35px h-md-40px position-relative w-35px w-md-40px'
    );
    readonly iconStyle = input('flaticon-alert-2 unread-notification fs-4');

    readonly notifications = signal<IFormattedUserNotification[]>([]);
    readonly unreadNotificationCount = signal<number>(0);

    ngOnInit(): void {
        this.loadNotifications();
        this.registerToEvents();
        this.startPeriodicRefresh();

        // ONCE, not on every reload. This raises a "a new version is available, reload?" confirm, and it
        // used to hang off loadNotifications -- which now runs on a timer and on every arriving
        // notification, so leaving it there would put that dialog in front of people once a minute.
        this.shouldUserUpdateApp();
    }

    private startPeriodicRefresh(): void {
        if (UrlHelper.isInstallUrl(location.href)) {
            return;
        }

        const handle = setInterval(() => this.loadNotifications(), HeaderNotificationsComponent.RefreshIntervalMs);

        // Cleared on destroy. The bell lives in the layout and is destroyed on logout; an interval left
        // running would go on polling an endpoint the user is no longer authorised for, and the 401s
        // would look like an attack in the logs.
        this._destroyRef.onDestroy(() => clearInterval(handle));
    }

    /**
     * Re-read on open, so what you are looking at is what is true.
     *
     * Costs one request per click and removes the whole class of "the badge disagrees with the list"
     * complaints: whatever happened while the page sat idle, opening the panel settles it.
     */
    onPanelShow(): void {
        this.loadNotifications();
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

    /**
     * Delete a single notification from the dropdown.
     *
     * NOT CONFIRMED, unlike the full page's row delete. Reading a notification is the whole of its
     * lifecycle -- there is nothing behind it to lose, the underlying request is untouched -- and a
     * confirm dialog on a three-item dropdown costs more than it protects. Deleting everything is
     * confirmed, because that one cannot be undone by waiting for the next notification.
     */
    deleteNotification(userNotification: IFormattedUserNotification): void {
        // No explicit reload: the helper raises app.notifications.refresh, which this component already
        // listens for. Reloading here as well would fire the same request twice.
        this._userNotificationHelper.deleteNotification(userNotification.userNotificationId);
    }

    deleteAllNotifications(): void {
        this.message.confirm(
            this.l('DeleteAllNotificationsWarningMessage'),
            this.l('AreYouSure'),
            (isConfirmed: boolean) => {
                if (isConfirmed) {
                    this._userNotificationHelper.deleteAll(() => this.notify.success(this.l('SuccessfullyDeleted')));
                }
            }
        );
    }

    gotoUrl(url): void {
        if (url) {
            location.href = url;
        }
    }
}
