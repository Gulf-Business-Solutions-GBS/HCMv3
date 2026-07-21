import { Component, OnInit, AfterViewInit, inject, viewChild, signal } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { SubscriptionStartType } from '@shared/service-proxies/service-proxies';
import { ChatSignalrService } from 'app/shared/layout/chat/chat-signalr.service';
import { AppComponentBase } from 'shared/common/app-component-base';
import { SignalRHelper } from 'shared/helpers/SignalRHelper';
import { LinkedAccountsModalComponent } from '@app/shared/layout/linked-accounts-modal.component';
import { UserDelegationsModalComponent } from '@app/shared/layout/user-delegations-modal.component';
import { ChangePasswordModalComponent } from '@app/shared/layout/profile/change-password-modal.component';
import { ChangeProfilePictureModalComponent } from '@app/shared/layout/profile/change-profile-picture-modal.component';
import { MySettingsModalComponent } from '@app/shared/layout/profile/my-settings-modal.component';
import { NotificationSettingsModalComponent } from '@app/shared/layout/notifications/notification-settings-modal.component';
import { UserNotificationHelper } from '@app/shared/layout/notifications/UserNotificationHelper';
import { DateTimeService } from './shared/common/timing/date-time.service';

import {
    ToggleComponent,
    ScrollTopComponent,
    DrawerComponent,
    StickyComponent,
    MenuComponent,
    ScrollComponent,
} from '@metronic/app/kt/components';
import { NgClass } from '@angular/common';
import { DefaultLayoutComponent } from './shared/layout/themes/default/default-layout.component';
import { Theme2LayoutComponent } from './shared/layout/themes/theme2/theme2-layout.component';
import { Theme3LayoutComponent } from './shared/layout/themes/theme3/theme3-layout.component';
import { Theme4LayoutComponent } from './shared/layout/themes/theme4/theme4-layout.component';
import { Theme5LayoutComponent } from './shared/layout/themes/theme5/theme5-layout.component';
import { Theme6LayoutComponent } from './shared/layout/themes/theme6/theme6-layout.component';
import { Theme7LayoutComponent } from './shared/layout/themes/theme7/theme7-layout.component';
import { Theme8LayoutComponent } from './shared/layout/themes/theme8/theme8-layout.component';
import { Theme9LayoutComponent } from './shared/layout/themes/theme9/theme9-layout.component';
import { Theme10LayoutComponent } from './shared/layout/themes/theme10/theme10-layout.component';
import { Theme11LayoutComponent } from './shared/layout/themes/theme11/theme11-layout.component';
import { Theme12LayoutComponent } from './shared/layout/themes/theme12/theme12-layout.component';
import { Theme13LayoutComponent } from './shared/layout/themes/theme13/theme13-layout.component';
import { ScrollTopComponent as ScrollTopComponent_1 } from './shared/layout/scroll-top.component';
import { LinkedAccountsModalComponent as LinkedAccountsModalComponent_1 } from './shared/layout/linked-accounts-modal.component';
import { UserDelegationsModalComponent as UserDelegationsModalComponent_1 } from './shared/layout/user-delegations-modal.component';
import { ChangePasswordModalComponent as ChangePasswordModalComponent_1 } from './shared/layout/profile/change-password-modal.component';
import { ChangeProfilePictureModalComponent as ChangeProfilePictureModalComponent_1 } from './shared/layout/profile/change-profile-picture-modal.component';
import { MySettingsModalComponent as MySettingsModalComponent_1 } from './shared/layout/profile/my-settings-modal.component';
import { NotificationSettingsModalComponent as NotificationSettingsModalComponent_1 } from './shared/layout/notifications/notification-settings-modal.component';
import { AddFriendModalComponent } from './shared/layout/chat/add-friend-modal.component';
import { ChatBarComponent } from './shared/layout/chat/chat-bar.component';
import { ThemeSelectionPanelComponent } from './shared/layout/theme-selection/theme-selection-panel.component';
import { SessionTimeoutComponent } from './shared/common/session-timeout/session-timeout.component';

@Component({
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.less'],
    imports: [
        NgClass,
        DefaultLayoutComponent,
        Theme2LayoutComponent,
        Theme3LayoutComponent,
        Theme4LayoutComponent,
        Theme5LayoutComponent,
        Theme6LayoutComponent,
        Theme7LayoutComponent,
        Theme8LayoutComponent,
        Theme9LayoutComponent,
        Theme10LayoutComponent,
        Theme11LayoutComponent,
        Theme12LayoutComponent,
        Theme13LayoutComponent,
        ScrollTopComponent_1,
        LinkedAccountsModalComponent_1,
        UserDelegationsModalComponent_1,
        ChangePasswordModalComponent_1,
        ChangeProfilePictureModalComponent_1,
        MySettingsModalComponent_1,
        NotificationSettingsModalComponent_1,
        AddFriendModalComponent,
        ChatBarComponent,
        ThemeSelectionPanelComponent,
        SessionTimeoutComponent,
    ],
})
export class AppComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private _chatSignalrService = inject(ChatSignalrService);
    private _userNotificationHelper = inject(UserNotificationHelper);
    private _dateTimeService = inject(DateTimeService);

    readonly linkedAccountsModal = viewChild<LinkedAccountsModalComponent>('linkedAccountsModal');
    readonly userDelegationsModal = viewChild<UserDelegationsModalComponent>('userDelegationsModal');
    readonly changePasswordModal = viewChild<ChangePasswordModalComponent>('changePasswordModal');
    readonly changeProfilePictureModal = viewChild<ChangeProfilePictureModalComponent>('changeProfilePictureModal');
    readonly mySettingsModal = viewChild<MySettingsModalComponent>('mySettingsModal');
    readonly notificationSettingsModal = viewChild<NotificationSettingsModalComponent>('notificationSettingsModal');
    readonly chatBarComponent = viewChild('chatBarComponent');

    subscriptionStartType = SubscriptionStartType;
    readonly theme = signal<string>('');
    readonly installationMode = signal<boolean>(true);
    readonly isQuickThemeSelectEnabled = signal<boolean>(
        this.setting.getBoolean('App.UserManagement.IsQuickThemeSelectEnabled')
    );
    readonly IsSessionTimeOutEnabled = signal<boolean>(
        this.setting.getBoolean('App.UserManagement.SessionTimeOut.IsEnabled') && this.appSession.userId != null
    );

    ngOnInit(): void {
        this._userNotificationHelper.settingsModal = this.notificationSettingsModal();
        this.theme.set(abp.setting.get('App.UiManagement.Theme').toLocaleLowerCase());
        this.installationMode.set(UrlHelper.isInstallUrl(location.href));

        this.registerModalOpenEvents();

        if (this.appSession.application) {
            SignalRHelper.initSignalR(() => {
                this._chatSignalrService.init();
            });
        }

        abp.event.on('app.dynamic-styles-loaded', function () {
            KTUtil.resize();
        });
    }

    ngAfterViewInit(): void {
        this.initializeMetronicPlugins();
    }

    pluginsInitialized(): boolean {
        let menuItems = document.querySelectorAll('[data-kt-menu="true"]');
        for (let i = 0; i < menuItems.length; i++) {
            let el = menuItems[i];
            const menuItem = el as HTMLElement;
            let menuInstance = MenuComponent.getInstance(menuItem);
            if (menuInstance) {
                return true;
            }
        }

        return false;
    }

    subscriptionStatusBarVisible(): boolean {
        return (
            this.appSession.tenantId > 0 &&
            (this.appSession.tenant.isInTrialPeriod || this.subscriptionIsExpiringSoon())
        );
    }

    subscriptionIsExpiringSoon(): boolean {
        if (this.appSession.tenant?.subscriptionEndDateUtc) {
            let daysFromNow = this._dateTimeService
                .getUTCDate()
                .plusDays(AppConsts.subscriptionExpireNootifyDayCount)
                .toDateTime();
            return daysFromNow >= this.appSession.tenant.subscriptionEndDateUtc;
        }

        return false;
    }

    registerModalOpenEvents(): void {
        this.subscribeToEvent('app.show.linkedAccountsModal', () => {
            this.linkedAccountsModal().show();
        });

        this.subscribeToEvent('app.show.userDelegationsModal', () => {
            this.userDelegationsModal().show();
        });

        this.subscribeToEvent('app.show.changePasswordModal', () => {
            this.changePasswordModal().show();
        });

        this.subscribeToEvent('app.show.changeProfilePictureModal', () => {
            this.changeProfilePictureModal().show();
        });

        this.subscribeToEvent('app.show.mySettingsModal', () => {
            this.mySettingsModal().show();
        });
    }

    getRecentlyLinkedUsers(): void {
        abp.event.trigger('app.getRecentlyLinkedUsers');
    }

    onMySettingsModalSaved(): void {
        abp.event.trigger('app.onMySettingsModalSaved');
    }

    private initializeMetronicPlugins(): void {
        queueMicrotask(() => {
            if (this.pluginsInitialized()) {
                return;
            }

            ToggleComponent.bootstrap();
            ScrollTopComponent.bootstrap();
            DrawerComponent.bootstrap();
            StickyComponent.bootstrap();
            MenuComponent.bootstrap();
            ScrollComponent.bootstrap();
        });
    }
}
