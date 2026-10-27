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
        const readBreakpoint = () => {
            const isMobile = this.mobileQuery.matches;
            this.isMobileViewport.set(isMobile);

            if (!isMobile) {
                this.closeMobileSidebar();
            }
        };

        const onBreakpointChange = () => readBreakpoint();

        this.mobileQuery.addEventListener('change', onBreakpointChange);

        /*
         * Resize as well as the media query's own event, because the event alone was not reliable: the query
         * read "phone" while this flag was still false, so the sidebar kept its desktop markup -- no drawer, a
         * close button laid out to nothing, and a menu that covered the page with no way to shut it. Seen on a
         * window that started wide and was narrowed, and on a phone browser reporting its size late.
         *
         * Reading matches() on resize costs nothing (the browser has the answer already) and the signal only
         * notifies when the value actually changes, so a drag across the breakpoint updates once, not per pixel.
         */
        window.addEventListener('resize', onBreakpointChange);

        /*
         * And a ResizeObserver on the page element, because on a phone neither of the above is dependable.
         * A phone browser resizes the page when its address bar slides away, when the keyboard opens, and on
         * rotation -- and reports those at its own pace, sometimes without a resize event and sometimes before
         * it has settled on a size. The observer fires on the element actually changing size, which is the one
         * thing that cannot be missed. Belt and braces, deliberately: this flag being wrong leaves a menu on a
         * phone that cannot be closed, which was reported from a real handset half a dozen times.
         */
        const observer = new ResizeObserver(() => readBreakpoint());
        observer.observe(this.document.documentElement);

        /*
         * The close, listened for on the document itself rather than through the template's bindings.
         *
         * On a real handset the X took the tap -- it turned its active blue -- and nothing happened, with the
         * current stylesheet confirmed on the server. The tap reaches the element; the binding attached to it
         * does not run. Rather than keep guessing why, this listens where nothing can get between: on the
         * document, in the capture phase, before any handler in between can swallow the event.
         *
         * Both events, because a phone may send one or the other: touchend always, click only sometimes.
         * Closing is idempotent, so getting both costs nothing. The template's own bindings stay as they are --
         * they work everywhere else, and two paths to the same idempotent close cannot collide.
         */
        const closeFromAnywhere = (event: Event) => {
            const target = event.target as HTMLElement | null;
            if (!target) {
                return;
            }

            // Opening is heard here too, so the menu can always be brought back: the stylesheet now hides it
            // on a phone until it is opened, and if only the close went through this listener a swallowed tap
            // on the hamburger would leave somebody with no menu at all.
            if (target.closest?.('#kt_app_sidebar_mobile_toggle')) {
                this.openMobileSidebar();
                return;
            }

            if (!this.isMobileSidebarOpen()) {
                return;
            }

            const onBackdrop = target.classList?.contains('app-sidebar-backdrop');
            const onCloseButton = !!target.closest?.('#kt_app_sidebar_logo button');

            if (onBackdrop || onCloseButton) {
                this.closeMobileSidebar();
            }
        };

        this.document.addEventListener('touchend', closeFromAnywhere, true);
        this.document.addEventListener('click', closeFromAnywhere, true);

        inject(DestroyRef).onDestroy(() => {
            this.mobileQuery.removeEventListener('change', onBreakpointChange);
            window.removeEventListener('resize', onBreakpointChange);
            observer.disconnect();
            this.document.removeEventListener('touchend', closeFromAnywhere, true);
            this.document.removeEventListener('click', closeFromAnywhere, true);
        });

        // The width now, not the width when this object was built: the layout can be created before the
        // browser has settled on its final size, and that reading is the one that stuck.
        readBreakpoint();

        // Stop the page scrolling underneath an open menu -- otherwise a swipe on the menu scrolls the
        // dashboard behind it instead.
        effect(() => {
            /*
             * Keyed on the menu being open ALONE, not on this layout also believing it is on a phone. The menu
             * can only be opened from the hamburger, and the hamburger only exists below lg, so "open" already
             * means "on a phone" -- while isMobileViewport is a reading that has twice been stale. The stylesheet
             * hangs the whole off-canvas menu off this class, so a wrong reading can no longer produce a menu
             * that covers the page with no way out.
             */
            const locked = this.isMobileSidebarOpen();
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

    /*
     * Gone on purpose: the hamburger OPENS, it does not toggle.
     *
     * While it toggled, the hamburger had two handlers -- the template's binding and the document-level
     * listener that was added so a phone could not swallow the tap. On a phone only one of them ran and the
     * menu opened; on a desktop narrowed to the same width both ran, the second undid the first, and the
     * hamburger did nothing at all. Two paths to "open" cannot cancel each other; two paths to "toggle" must.
     * The menu is closed by the X, the backdrop, Escape, or tapping a page in it -- never by this button.
     */

    /**
     * Tapping a page in the open menu closes it, whether or not the page changes.
     *
     * Closing on NavigationEnd alone missed one case: tapping the page you are already on. The router does
     * not navigate to the current URL, so no NavigationEnd arrives and the menu stayed over the page it had
     * just "opened" -- on a phone that looks exactly like a menu that will not go away. A group header
     * (.menu-toggle) only expands its children, and the favourite star stops its own click, so neither
     * reaches here as a page tap.
     */
    onSidebarClick(event: MouseEvent): void {
        if (!this.isMobileSidebarOpen()) {
            return;
        }

        const link = (event.target as HTMLElement | null)?.closest('a.menu-link');
        if (link && !link.classList.contains('menu-toggle')) {
            this.closeMobileSidebar();
        }
    }

    /**
     * The X and the backdrop close on the finger lifting, not on the click that iOS may or may not send after.
     *
     * iPhone Safari (and Chrome on iPhone, which is Safari underneath) turns some taps into a hover and never
     * fires the click -- the X went blue, its hover colour, and the menu stayed open. touchend always fires.
     * preventDefault stops the click that would otherwise follow; without it that click would land on whatever
     * was under the finger once the menu had slid away. Mouse and keyboard still close through (click).
     */
    closeMobileSidebarOnTouch(event: TouchEvent): void {
        // In a passive listener this logs a warning instead of working, so the close below must not depend on
        // it -- and does not.
        if (event.cancelable) {
            event.preventDefault();
        }

        this.closeMobileSidebar();
    }

    /**
     * Idempotent: closing a menu that is already closed does nothing, so no two close paths can collide.
     *
     * The signal is the truth, and on every browser reached from this machine setting it is enough. On real
     * handsets it was not: the menu stayed over the page with the X reported as tapped, half a dozen times,
     * on a build whose stylesheet was confirmed current. That leaves the render as the suspect -- the state
     * changing without the page being redrawn -- which is not something that can be debugged from here.
     *
     * So the classes that hold the menu open are also taken off directly. The template still owns them and
     * will put things right on its next pass; this only means a tap cannot leave somebody trapped while that
     * is pending. Deliberately boring DOM calls: no framework, nothing to go stale.
     */
    closeMobileSidebar(): void {
        this.isMobileSidebarOpen.set(false);

        this.document.body.classList.remove('hcm-mobile-menu-open');
        this.document.getElementById('kt_app_sidebar')?.classList.remove('drawer-on');
        this.document.querySelector('.app-sidebar-backdrop')?.remove();
    }

    /**
     * Opening, with the same belt and braces as closing: the class goes on by hand as well as through the
     * signal. The stylesheet hides the menu on a phone until that class is there, so if the redraw were the
     * thing failing -- which is still not ruled out -- the menu would otherwise never appear at all.
     */
    openMobileSidebar(): void {
        this.isMobileSidebarOpen.set(true);

        this.document.body.classList.add('hcm-mobile-menu-open');
        this.document.getElementById('kt_app_sidebar')?.classList.add('drawer-on');
    }
}
