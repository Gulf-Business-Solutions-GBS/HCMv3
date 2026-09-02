import { PermissionCheckerService } from 'abp-ng2-module';
import { Component, OnInit, ViewEncapsulation, AfterViewInit, inject, input, computed } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppMenu } from './app-menu';
import { AppNavigationService } from './app-navigation.service';
import { ScreenNavigationService } from './screen-navigation.service';
import { ScreenDto } from '@shared/service-proxies/service-proxies';
import { NavigationEnd, NavigationCancel, Router, RouterLinkActive, RouterLink } from '@angular/router';
import { filter } from 'rxjs/operators';
import { FormattedStringValueExtracter } from '@shared/helpers/FormattedStringValueExtracter';
import * as objectPath from 'object-path';
import { AppMenuItem } from './app-menu-item';
import { MenuComponent, DrawerComponent, ToggleComponent, ScrollComponent } from '@metronic/app/kt/components';
import { NgTemplateOutlet, NgClass } from '@angular/common';
import { MenuSearchBarComponent } from './menu-search-bar/menu-search-bar.component';
import { Tooltip } from 'primeng/tooltip';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './side-bar-menu.component.html',
    selector: 'side-bar-menu',
    styleUrl: './side-bar-menu.component.css',
    encapsulation: ViewEncapsulation.None,
    imports: [MenuSearchBarComponent, NgTemplateOutlet, NgClass, RouterLinkActive, Tooltip, RouterLink, LocalizePipe],
})
export class SideBarMenuComponent extends AppComponentBase implements OnInit, AfterViewInit {
    private router = inject(Router);
    private _appNavigationService = inject(AppNavigationService);
    private _screenNavigation = inject(ScreenNavigationService);

    permission = inject(PermissionCheckerService);

    readonly iconMenu = input(false);
    readonly menuClass = input('menu menu-column menu-rounded menu-sub-indention px-3');

    menu: AppMenu = null;

    /**
     * The catalogued screen behind a menu item, or undefined for a group header -- a group is not a
     * screen, so it has neither a code nor anything to favourite.
     */
    screenFor(item: AppMenuItem): ScreenDto | undefined {
        return item?.route ? this._screenNavigation.screenForRoute(item.route) : undefined;
    }

    /** The code to suffix, or undefined when the user has codes switched off. */
    screenCodeFor(item: AppMenuItem): string | undefined {
        return this._screenNavigation.showScreenCodes() ? this.screenFor(item)?.code : undefined;
    }

    isFavorite(screenKey: string): boolean {
        return this._screenNavigation.isFavorite(screenKey);
    }

    showScreenCodes(): boolean {
        return this._screenNavigation.showScreenCodes();
    }

    favoriteScreens(): ScreenDto[] {
        return this._screenNavigation.favoriteScreens();
    }

    /**
     * The five most recent, minus anything already pinned above -- showing a screen twice in two lists
     * eight pixels apart wastes the space both lists are competing for.
     */
    recentScreens(): ScreenDto[] {
        const pinned = new Set(this._screenNavigation.favorites().map((f) => f.screenKey));

        return this._screenNavigation
            .recent()
            .filter((r) => !pinned.has(r.screenKey))
            .map((r) => this._screenNavigation.screenForKey(r.screenKey))
            .filter((s): s is ScreenDto => !!s);
    }

    /**
     * Stops the click reaching the anchor. Without this, starring a screen also navigates to it -- the
     * star sits inside the link, and pinning something is not a request to go there.
     */
    toggleFavorite(event: MouseEvent, screen: ScreenDto): void {
        event.preventDefault();
        event.stopPropagation();
        this._screenNavigation.toggleFavorite(screen);
    }

    currentRouteUrl = '';
    insideTm: any;
    outsideTm: any;

    visibleMenuItems = computed(() => {
        return this.menu.items.filter((child) => this.showMenuItem(child));
    });

    ngOnInit() {
        this.menu = this._appNavigationService.getMenu();
        this.currentRouteUrl = this.router.url.split(/[?#]/)[0];

        // The sidebar is on every authenticated page and is the first thing that needs codes, so it is
        // where the catalogue is fetched. Guarded because the shell can rebuild this component.
        if (!this._screenNavigation.isLoaded()) {
            this._screenNavigation.load();
        }

        this.router.events
            .pipe(filter((event) => event instanceof NavigationEnd))
            .subscribe((event) => {
                this.currentRouteUrl = this.router.url.split(/[?#]/)[0];

                // Recorded on arrival rather than on click, so a screen reached by URL, bookmark or
                // quick link counts the same as one reached from the menu.
                this._screenNavigation.recordVisit(this.currentRouteUrl);
            });

        this.router.events
            .pipe(filter((event) => event instanceof NavigationEnd || event instanceof NavigationCancel))
            .subscribe((event) => {
                this.reinitializeMenu();
            });
    }

    ngAfterViewInit(): void {
        this.scrollToCurrentMenuElement();
    }

    reinitializeMenu(): void {
        this.menu = this._appNavigationService.getMenu();
        this.currentRouteUrl = this.router.url.split(/[?#]/)[0];

        setTimeout(() => {
            MenuComponent.reinitialization();
            DrawerComponent.reinitialization();
            ToggleComponent.reinitialization();
            ScrollComponent.reinitialization();
        }, 50);
    }

    showMenuItem(menuItem): boolean {
        return this._appNavigationService.showMenuItem(menuItem);
    }

    getTriggerCssClass(item, parentItem): string {
        if (!item.items && item.items.length <= 0) {
            return '';
        }

        if (this.iconMenu() && parentItem == null) {
            return 'hover';
        }

        return 'click';
    }

    isMenuItemIsActive(item): boolean {
        if (item.items.length) {
            return this.isMenuRootItemIsActive(item);
        }

        if (!item.route) {
            return false;
        }

        let urlTree = this.router.parseUrl(this.currentRouteUrl.replace(/\/$/, ''));
        let urlString = '/' + urlTree.root.children.primary.segments.map((segment) => segment.path).join('/');
        let exactMatch = urlString === item.route.replace(/\/$/, '');
        if (!exactMatch && item.routeTemplates) {
            for (let i = 0; i < item.routeTemplates.length; i++) {
                let result = new FormattedStringValueExtracter().Extract(urlString, item.routeTemplates[i]);
                if (result.IsMatch) {
                    return true;
                }
            }
        }
        return exactMatch;
    }

    isMenuRootItemIsActive(item): boolean {
        for (const subItem of item.items) {
            if (this.isMenuItemIsActive(subItem)) {
                return true;
            }
        }

        return false;
    }

    scrollToCurrentMenuElement(): void {
        const path = location.pathname;
        const menuItem = document.querySelector(`a[href='${path}']`);
        if (menuItem) {
            menuItem.scrollIntoView({ block: 'center' });
        }
    }

    getItemCssClasses(item: AppMenuItem, parentItem: AppMenuItem) {
        let classes = 'menu-item';

        const iconMenu = this.iconMenu();
        if (item.items.length) {
            if (!iconMenu) {
                classes += ' menu-accordion';
            } else {
                if (parentItem == null) {
                    classes += ' menu-dropdown';
                } else {
                    classes += ' menu-accordion';
                }
            }
        }

        // custom class for menu item
        const customClass = objectPath.get(item, 'custom-class');
        if (customClass) {
            classes += ' ' + customClass;
        }

        if (iconMenu && parentItem == null) {
            classes += ' pb-3';
        }

        if (!iconMenu && this.isMenuItemIsActive(item)) {
            classes += ' show';
        }

        return classes;
    }

    getSubMenuItemCssClass(item: AppMenuItem, parentItem: AppMenuItem): string {
        let classes = 'menu-sub';

        if (!this.iconMenu()) {
            classes += ' menu-sub-accordion';
        } else {
            if (parentItem == null) {
                classes += ' menu-sub-dropdown px-1 py-4';
            } else {
                classes += ' menu-sub-accordion';
            }
        }

        return classes;
    }

    openExternalUrl(url: string): void {
        if (url) {
            window.open(url, '_blank');
        }
    }
}
