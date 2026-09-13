import { Component, DestroyRef, DOCUMENT, HostListener, OnInit, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs/operators';

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
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
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
    private router = inject(Router);

    /** Metronic's lg breakpoint. Below it the sidebar is an off-canvas menu; at or above it, a fixed column. */
    private readonly mobileQuery = window.matchMedia('(max-width: 991.98px)');

    /** Whether the viewport is below lg, so the sidebar should behave as an off-canvas menu. */
    readonly isMobileViewport = signal(this.mobileQuery.matches);

    /**
     * Whether the off-canvas menu is showing. The single source of truth for it.
     *
     * Metronic's drawer script used to own this, and nothing in the app could reach it -- its instance is
     * sealed inside the bundle. So every way of closing had to fake a click on the hamburger, which made
     * "close" a toggle in disguise: fired when the menu was already shut, it OPENED it. On iPhone Safari the
     * delegated listener behind that fake click is also exactly what a tap can fail to reach. Owning the
     * state here makes every close path a plain, idempotent set(false) that cannot undo another.
     */
    readonly isMobileSidebarOpen = signal(false);

    constructor() {
        super();

        // Tapping a menu item has to reveal the page it opened. Without this the menu stayed on top of the
        // screen it had just navigated to, which is what made the app unusable on a phone.
        this.router.events
            .pipe(
                filter((event) => event instanceof NavigationEnd),
                takeUntilDestroyed()
            )
            .subscribe(() => this.closeMobileSidebar());

        // Crossing into desktop width closes the menu, so rotating a tablet or widening a window never
        // leaves the page locked behind a menu the layout no longer treats as one.
        const onBreakpointChange = (event: MediaQueryListEvent) => {
            this.isMobileViewport.set(event.matches);

            if (!event.matches) {
                this.closeMobileSidebar();
            }
        };

        this.mobileQuery.addEventListener('change', onBreakpointChange);
        inject(DestroyRef).onDestroy(() => this.mobileQuery.removeEventListener('change', onBreakpointChange));

        // Stop the page scrolling underneath an open menu -- otherwise a swipe on the menu scrolls the
        // dashboard behind it instead.
        effect(() => {
            const locked = this.isMobileViewport() && this.isMobileSidebarOpen();
            this.document.body.classList.toggle('hcm-mobile-menu-open', locked);
        });
    }

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

        if (this.isMobileSidebarOpen()) {
            this.closeMobileSidebar();
        }
    }

    toggleMobileSidebar(): void {
        this.isMobileSidebarOpen.update((open) => !open);
    }

    /** Idempotent: closing a menu that is already closed does nothing, so no two close paths can collide. */
    closeMobileSidebar(): void {
        this.isMobileSidebarOpen.set(false);
    }
}
