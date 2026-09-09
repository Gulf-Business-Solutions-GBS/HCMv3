import { Component, HostListener, OnInit, DOCUMENT, inject, signal } from '@angular/core';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ThemesLayoutBaseComponent } from '@app/shared/layout/themes/themes-layout-base.component';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { AppConsts } from '@shared/AppConsts';
import { DefaultLogoComponent } from './default-logo.component';
import { TopBarMenuComponent } from '../../nav/top-bar-menu.component';
import { ActiveDelegatedUsersComboComponent } from '../../topbar/active-delegated-users-combo.component';
import { SubscriptionNotificationBarComponent } from '../../topbar/subscription-notification-bar.component';
import { QuickThemeSelectionComponent } from '../../topbar/quick-theme-selection.component';
import { LanguageSwitchDropdownComponent } from '../../topbar/language-switch-dropdown.component';
import { HeaderNotificationsComponent } from '../../notifications/header-notifications.component';
import { ChatToggleButtonComponent } from '../../topbar/chat-toggle-button.component';
import { ToggleDarkModeComponent } from '../../toggle-dark-mode/toggle-dark-mode.component';
import { UserMenuComponent } from '../../topbar/user-menu.component';
import { DefaultBrandComponent } from './default-brand.component';
import { SideBarMenuComponent } from '../../nav/side-bar-menu.component';
import { RouterOutlet } from '@angular/router';
import { FooterComponent } from '../../footer.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { MenuSearchBarComponent } from '../../nav/menu-search-bar/menu-search-bar.component';

@Component({
    templateUrl: './default-layout.component.html',
    selector: 'default-layout',
    animations: [appModuleAnimation()],
    imports: [
        DefaultLogoComponent,
        TopBarMenuComponent,
        ActiveDelegatedUsersComboComponent,
        SubscriptionNotificationBarComponent,
        QuickThemeSelectionComponent,
        LanguageSwitchDropdownComponent,
        HeaderNotificationsComponent,
        ChatToggleButtonComponent,
        ToggleDarkModeComponent,
        UserMenuComponent,
        DefaultBrandComponent,
        SideBarMenuComponent,
        RouterOutlet,
        FooterComponent,
        LocalizePipe,
        MenuSearchBarComponent,
    ],
})
export class DefaultLayoutComponent extends ThemesLayoutBaseComponent implements OnInit {
    private document = inject<Document>(DOCUMENT);

    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
    skin: string = this.appSession.theme.baseSettings.layout.darkMode ? 'dark' : 'light';

    ngOnInit() {
        this.installationMode = UrlHelper.isInstallUrl(location.href);
        if (this.currentTheme.baseSettings.menu.defaultMinimizedAside) {
            this.document.body.setAttribute('data-kt-aside-minimize', 'on');
        }
    }

    /**
     * Whether the mobile search overlay is showing. Only ever true below lg -- the icon that sets it is
     * itself d-lg-none, and the overlay is too, so a desktop window can never end up holding it open.
     */
    readonly isMobileSearchOpen = signal(false);

    openMobileSearch(): void {
        this.isMobileSearchOpen.set(true);

        // Focus after the overlay has actually rendered -- the input does not exist until change
        // detection has run, and a phone keyboard that does not appear makes the overlay look broken.
        setTimeout(() => {
            const input = this.document.querySelector<HTMLInputElement>('.mobile-search-overlay input');
            input?.focus();
        }, 50);
    }

    closeMobileSearch(): void {
        this.isMobileSearchOpen.set(false);
    }

    /** Escape closes the overlay, which is what anyone with a keyboard attached will try first. */
    @HostListener('document:keydown.escape')
    onEscape(): void {
        if (this.isMobileSearchOpen()) {
            this.closeMobileSearch();
        }
    }

    /**
     * Closes the mobile sidebar drawer by clicking the header toggle that owns it, rather than reaching
     * into Metronic's drawer API -- the toggle already carries the open/close behaviour, so routing through
     * it keeps one source of truth and cannot fall out of step with the data-kt-drawer wiring.
     */
    closeMobileSidebar(): void {
        this.document.getElementById('kt_app_sidebar_mobile_toggle')?.click();
    }
}
