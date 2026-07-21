import { ElementRef, Component, OnInit, viewChild } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ThemesLayoutBaseComponent } from '@app/shared/layout/themes/themes-layout-base.component';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { AppConsts } from '@shared/AppConsts';
import { Theme4BrandComponent } from './theme4-brand.component';
import { TopBarMenuComponent } from '../../nav/top-bar-menu.component';
import { ActiveDelegatedUsersComboComponent } from '../../topbar/active-delegated-users-combo.component';
import { SubscriptionNotificationBarComponent } from '../../topbar/subscription-notification-bar.component';
import { QuickThemeSelectionComponent } from '../../topbar/quick-theme-selection.component';
import { LanguageSwitchDropdownComponent } from '../../topbar/language-switch-dropdown.component';
import { HeaderNotificationsComponent } from '../../notifications/header-notifications.component';
import { ChatToggleButtonComponent } from '../../topbar/chat-toggle-button.component';
import { UserMenuComponent } from '../../topbar/user-menu.component';
import { RouterOutlet } from '@angular/router';
import { FooterComponent } from '../../footer.component';

@Component({
    templateUrl: './theme4-layout.component.html',
    selector: 'theme4-layout',
    animations: [appModuleAnimation()],
    imports: [
        Theme4BrandComponent,
        TopBarMenuComponent,
        ActiveDelegatedUsersComboComponent,
        SubscriptionNotificationBarComponent,
        QuickThemeSelectionComponent,
        LanguageSwitchDropdownComponent,
        HeaderNotificationsComponent,
        ChatToggleButtonComponent,
        UserMenuComponent,
        RouterOutlet,
        FooterComponent,
    ],
})
export class Theme4LayoutComponent extends ThemesLayoutBaseComponent implements OnInit {
    readonly ktHeader = viewChild<ElementRef>('ktHeader');

    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;

    ngOnInit() {
        this.installationMode = UrlHelper.isInstallUrl(location.href);
    }
}
