import { ElementRef, Component, OnInit, AfterViewInit, DOCUMENT, inject, viewChild } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ThemesLayoutBaseComponent } from '@app/shared/layout/themes/themes-layout-base.component';
import { UrlHelper } from '@shared/helpers/UrlHelper';

import { AppConsts } from '@shared/AppConsts';
import { Theme2BrandComponent } from './theme2-brand.component';
import { TopBarMenuComponent } from '../../nav/top-bar-menu.component';
import { ActiveDelegatedUsersComboComponent } from '../../topbar/active-delegated-users-combo.component';
import { SubscriptionNotificationBarComponent } from '../../topbar/subscription-notification-bar.component';
import { QuickThemeSelectionComponent } from '../../topbar/quick-theme-selection.component';
import { LanguageSwitchDropdownComponent } from '../../topbar/language-switch-dropdown.component';
import { HeaderNotificationsComponent } from '../../notifications/header-notifications.component';
import { ChatToggleButtonComponent } from '../../topbar/chat-toggle-button.component';
import { ToggleDarkModeComponent } from '../../toggle-dark-mode/toggle-dark-mode.component';
import { UserMenuComponent } from '../../topbar/user-menu.component';
import { RouterOutlet } from '@angular/router';
import { FooterComponent } from '../../footer.component';

@Component({
    templateUrl: './theme2-layout.component.html',
    selector: 'theme2-layout',
    animations: [appModuleAnimation()],
    imports: [
        Theme2BrandComponent,
        TopBarMenuComponent,
        ActiveDelegatedUsersComboComponent,
        SubscriptionNotificationBarComponent,
        QuickThemeSelectionComponent,
        LanguageSwitchDropdownComponent,
        HeaderNotificationsComponent,
        ChatToggleButtonComponent,
        ToggleDarkModeComponent,
        UserMenuComponent,
        RouterOutlet,
        FooterComponent,
    ],
})
export class Theme2LayoutComponent extends ThemesLayoutBaseComponent implements OnInit, AfterViewInit {
    private document = inject<Document>(DOCUMENT);

    readonly ktHeader = viewChild<ElementRef>('ktHeader');

    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    ngOnInit() {
        this.installationMode = UrlHelper.isInstallUrl(location.href);
    }

    ngAfterViewInit(): void {}

    toggleLeftAside(): void {
        this.document.body.classList.toggle('header-menu-wrapper-on');
        this.document.getElementById('kt_header_menu_wrapper').classList.toggle('header-menu-wrapper-on');
    }
}
