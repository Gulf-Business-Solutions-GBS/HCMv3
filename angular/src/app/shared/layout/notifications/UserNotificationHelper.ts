import { Injectable, inject } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EntityDtoOfGuid,
    NotificationServiceProxy,
    NotificationSeverity,
    SetNotificationAsReadOutput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import * as Push from 'push.js'; // if using ES6
import { NotificationSettingsModalComponent } from './notification-settings-modal.component';
import { ViewNotificationModalComponent } from './view-notification-modal.component';
import { AppConsts } from '@shared/AppConsts';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

export interface IFormattedUserNotification {
    userNotificationId: string;
    text: string;
    time: string;
    creationTime: DateTime;
    icon: string;
    state: String;
    data: any;
    url: string;
    isUnread: boolean;
    severity: abp.notifications.severity;
    iconFontClass: string;
}

@Injectable()
export class UserNotificationHelper extends AppComponentBase {
    private _notificationService = inject(NotificationServiceProxy);
    private _dateTimeService = inject(DateTimeService);
    private http = inject(HttpClient);
    private _router = inject(Router);

    settingsModal: NotificationSettingsModalComponent;
    viewModal: ViewNotificationModalComponent;

    getUrl(userNotification: abp.notifications.IUserNotification): string {
        switch (userNotification.notification.notificationName) {
            case 'App.NewUserRegistered':
                return '/app/admin/users?filterText=' + userNotification.notification.data.properties.emailAddress;
            case 'App.NewTenantRegistered':
                return '/app/admin/tenants?filterText=' + userNotification.notification.data.properties.tenancyName;
            case 'App.GdprDataPrepared':
                return (
                    AppConsts.remoteServiceBaseUrl +
                    '/File/DownloadBinaryFile?id=' +
                    userNotification.notification.data.properties.binaryObjectId +
                    '&contentType=application/zip&fileName=collectedData.zip'
                );
            case 'App.DownloadInvalidImportUsers':
                return (
                    AppConsts.remoteServiceBaseUrl +
                    '/File/DownloadTempFile?fileToken=' +
                    userNotification.notification.data.properties.fileToken +
                    '&fileType=' +
                    userNotification.notification.data.properties.fileType +
                    '&fileName=' +
                    userNotification.notification.data.properties.fileName
                );
            //Add your custom notification names to navigate to a URL when user clicks to a notification.
        }

        // Workflow notifications are matched on their DATA, not on their name. Today the dispatcher sends
        // them through IAppNotifier.SendMessageAsync, which publishes everything under the single name
        // 'App.SimpleMessage' -- so the name cannot tell a workflow notification apart from any other
        // one-line message, and switching on it would either catch nothing or catch everything.
        const workflowUrl = this.getWorkflowRequestUrl(userNotification);
        if (workflowUrl) {
            return workflowUrl;
        }

        //No url for this notification
        return '';
    }

    /**
     * Where a workflow notification should open, or '' when it carries nothing to open.
     *
     * <p><b>This is the client half of a two-part fix and currently returns '' for every real
     * notification.</b> WorkflowNotificationDispatcher publishes a MessageNotificationData, whose only
     * property is the rendered message string -- the request number is in the prose and nowhere in the
     * data. There is nothing to route on, so rather than parse "EXP-2026-00003" back out of a sentence
     * that is localised and reworded per action, this reads the ids the dispatcher ought to be sending
     * and stays quiet until they arrive. Publish <c>workflowRequestId</c> (the WorkflowRequest key, which
     * is what both list screens key their detail dialog on) and <c>isApprover</c>, and every clause below
     * starts working with no further client change.</p>
     *
     * <p>Approvers go to the inbox and requesters to their own request list, because those are the two
     * screens that can actually OPEN a request: both hold EssRequestDetailModalComponent and both key it
     * by workflow request id. The per-type ESS route is deliberately not used even when supplied -- those
     * screens list one transaction type and have no way to single a request out, so routing there would
     * land you on a list and call it done.</p>
     */
    private getWorkflowRequestUrl(userNotification: abp.notifications.IUserNotification): string {
        const properties = userNotification?.notification?.data?.properties;
        if (!properties) {
            return '';
        }

        // Both casings accepted: ABP serialises notification data as given, and whether a C# property
        // arrives as `workflowRequestId` or `WorkflowRequestId` depends on the serializer settings in
        // force, which is not something a click handler should be sensitive to.
        const requestId = properties.workflowRequestId ?? properties.WorkflowRequestId;
        if (!requestId) {
            return '';
        }

        const isApprover = properties.isApprover ?? properties.IsApprover;
        const screen = isApprover === false ? '/app/main/ess/requests' : '/app/main/ess/inbox';

        return screen + '?requestId=' + encodeURIComponent(String(requestId));
    }

    /**
     * Follow a notification's url.
     *
     * <p>Routes in-app addresses through the Router instead of assigning location.href. The old assignment
     * worked, in the sense that you ended up on the page, but it tore down and rebooted the whole Angular
     * application to move between two screens of it -- a blank second, a refetched menu, and any unsaved
     * state gone. Anything not starting with a slash is an absolute backend address (the GDPR and import
     * downloads below), and those still go through the browser.</p>
     */
    navigateToUrl(url: string): void {
        if (!url) {
            return;
        }

        if (url.startsWith('/')) {
            this._router.navigateByUrl(url);
            return;
        }

        location.href = url;
    }

    /* PUBLIC functions ******************************************/

    format(userNotification: abp.notifications.IUserNotification, truncateText?: boolean): IFormattedUserNotification {
        let formatted: IFormattedUserNotification = {
            userNotificationId: userNotification.id,
            text: abp.notifications.getFormattedMessageFromUserNotification(userNotification),
            time: this._dateTimeService.formatDate(userNotification.notification.creationTime, 'yyyy-LL-dd HH:mm:ss'),
            creationTime: userNotification.notification.creationTime as any,
            icon: this.getUiIconBySeverity(userNotification.notification.severity),
            state: abp.notifications.getUserNotificationStateAsString(userNotification.state),
            data: userNotification.notification.data,
            url: this.getUrl(userNotification),
            isUnread: userNotification.state === abp.notifications.userNotificationState.UNREAD,
            severity: userNotification.notification.severity,
            iconFontClass: this.getIconFontClassBySeverity(userNotification.notification.severity),
        };

        if (truncateText || truncateText === undefined) {
            formatted.text = abp.utils.truncateStringWithPostfix(formatted.text, 100);
        }

        return formatted;
    }

    show(userNotification: abp.notifications.IUserNotification): void {
        let url = this.getUrl(userNotification);
        //Application notification
        abp.notifications.showUiNotifyForUserNotification(userNotification, {
            didOpen: (toast) => {
                toast.addEventListener('click', () => {
                    //Take action when user clicks to live toastr notification
                    this.navigateToUrl(url);
                });
            },
        });
        if (Push.default.Permission.has()) {
            //Desktop notification
            Push.default.create('HCMv3', {
                body: this.format(userNotification).text,
                icon: abp.appPath + 'assets/common/images/app-logo-on-dark-sm.svg',
                timeout: 6000,
                onClick: function () {
                    window.focus();
                    this.close();
                },
            });
        }
    }

    //TODO: Remove this when ABP 9.2 is released.
    ensureEndsWith(str: string, c: string): string {
        if (!str) {
            return str;
        }

        if (str.endsWith(c)) {
            return str;
        }

        return str + c;
    }

    shouldUserUpdateApp(): void {
        this._notificationService.shouldUserUpdateApp().subscribe((result) => {
            if (result) {
                abp.message.confirm(null, this.l('NewVersionAvailableNotification'), (isConfirmed) => {
                    if (isConfirmed) {
                        this.http
                            .get(
                                this.ensureEndsWith(AppConsts.remoteServiceBaseUrl, '/') + 'BrowserCacheCleaner/Clear',
                                {
                                    withCredentials: true,
                                }
                            )
                            .subscribe((result: boolean) => {
                                if (result) {
                                    const url = new URL(location.href);
                                    url.searchParams.append('t', new Date().getTime().toString());
                                    location.href = url.toString();
                                }
                            });
                    }
                });
            }
        });
    }

    setAllAsRead(callback?: () => void): void {
        this._notificationService.setAllNotificationsAsRead().subscribe(() => {
            abp.event.trigger('app.notifications.refresh');
            if (callback) {
                callback();
            }
        });
    }

    setAsRead(userNotificationId: string, callback?: (userNotificationId: string) => void): void {
        const input = new EntityDtoOfGuid();
        input.id = userNotificationId;
        this._notificationService.setNotificationAsRead(input).subscribe((result: SetNotificationAsReadOutput) => {
            abp.event.trigger('app.notifications.read', userNotificationId, result.success);
            if (callback) {
                callback(userNotificationId);
            }
        });
    }

    /**
     * Delete one notification, then tell every bell on the page to re-read itself.
     *
     * The refresh event is the point of routing this through the helper rather than calling the proxy
     * from each screen: the header dropdown and the full notifications page can both be mounted at once,
     * and a delete performed in one of them left the other showing the row and counting it.
     */
    deleteNotification(userNotificationId: string, callback?: () => void): void {
        this._notificationService.deleteNotification(userNotificationId).subscribe(() => {
            abp.event.trigger('app.notifications.refresh');
            if (callback) {
                callback();
            }
        });
    }

    /**
     * Delete EVERY notification this user has, read or unread, whatever its date.
     *
     * Deliberately unfiltered, and that is the difference from the full page's "delete listed": that one
     * deletes what the date filter currently shows, which is the right verb there and the wrong one in a
     * dropdown, where nothing tells you what the hidden filter is. All three arguments are omitted so
     * the server applies no state or date restriction.
     */
    deleteAll(callback?: () => void): void {
        this._notificationService.deleteAllUserNotifications(undefined, undefined, undefined).subscribe(() => {
            abp.event.trigger('app.notifications.refresh');
            if (callback) {
                callback();
            }
        });
    }

    openSettingsModal(): void {
        this.settingsModal.show();
    }

    openViewNotificationModal(notificationId: string): void {
        this.viewModal.show(notificationId);
    }

    getUiIconBySeverity(severity: abp.notifications.severity): string {
        switch (severity) {
            case abp.notifications.severity.SUCCESS:
                return 'fas fa-check-circle';
            case abp.notifications.severity.WARN:
                return 'fas fa-exclamation-triangle';
            case abp.notifications.severity.ERROR:
                return 'fas fa-exclamation-circle';
            case abp.notifications.severity.FATAL:
                return 'fas fa-bomb';
            case abp.notifications.severity.INFO:
            default:
                return 'fas fa-info-circle';
        }
    }

    getIconFontClassBySeverity(severity: abp.notifications.severity): string {
        switch (severity) {
            case abp.notifications.severity.SUCCESS:
                return ' text-success';
            case abp.notifications.severity.WARN:
                return ' text-warning';
            case abp.notifications.severity.ERROR:
                return ' text-danger';
            case abp.notifications.severity.FATAL:
                return ' text-danger';
            case abp.notifications.severity.INFO:
            default:
                return ' text-info';
        }
    }

    getSeverityClass(severity: NotificationSeverity): string {
        switch (severity) {
            case NotificationSeverity.Success:
                return 'success';
            case NotificationSeverity.Warn:
                return 'warning';
            case NotificationSeverity.Error:
                return 'danger';
            case NotificationSeverity.Fatal:
                return 'danger';
            case NotificationSeverity.Info:
            default:
                return 'info';
        }
    }

    getSeverityIcon(severity: NotificationSeverity): string {
        switch (severity) {
            case NotificationSeverity.Success:
                return 'bi-check-circle';
            case NotificationSeverity.Warn:
                return 'bi-exclamation-triangle';
            case NotificationSeverity.Error:
                return 'bi-x-circle';
            case NotificationSeverity.Fatal:
                return 'bi-x-octagon';
            case NotificationSeverity.Info:
            default:
                return 'bi-info-circle';
        }
    }
}
