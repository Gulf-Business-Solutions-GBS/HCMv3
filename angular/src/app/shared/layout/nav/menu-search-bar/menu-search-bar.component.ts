import { Component, inject } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppNavigationService } from '../app-navigation.service';
import { ScreenNavigationService } from '../screen-navigation.service';
import { Router } from '@angular/router';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'menu-search-bar',
    templateUrl: './menu-search-bar.component.html',
    styleUrls: ['./menu-search-bar.component.css'],
    imports: [AutoCompleteModule, FormsModule, LocalizePipe],
})
export class MenuSearchBarComponent extends AppComponentBase {
    private _appNavigationService = inject(AppNavigationService);
    private _screenNavigation = inject(ScreenNavigationService);
    private router = inject(Router);

    allMenuItems: any[];

    searchMenuResults: any[];

    isMenuSearchActive = false;

    selected: '';

    constructor() {
        super();
        this.initializeMenuSearch();
    }

    showMenuItem(menuItem): boolean {
        return this._appNavigationService.showMenuItem(menuItem);
    }

    /**
     * Matches on name, route AND Screen Code, in the SAME box -- no second search field. Somebody who
     * knows a screen is SPY007 should be able to type it where they already type its name, and somebody
     * who does not should never notice codes exist here.
     */
    searchMenuItem(event) {
        const query = (event.query || '').toLowerCase();

        this.searchMenuResults = this.allMenuItems
            .map((item) => ({ ...item, code: this._screenNavigation.codeForRoute(item.route) }))
            .filter(
                (item) =>
                    item.name.toLowerCase().includes(query) ||
                    item.route.toLowerCase().includes(query) ||
                    (item.code && item.code.toLowerCase().includes(query))
            )
            .map((menuItem) => {
                return {
                    // The code rides in the label so a result is identifiable at a glance and the person
                    // learns the code of a screen they reached by name.
                    name: this.labelFor(menuItem.name, menuItem.code),
                    route: menuItem.route,
                    external: menuItem.external,
                    parameters: menuItem.parameters,
                };
            });
    }

    private labelFor(name: string, code: string | undefined): string {
        return code && this._screenNavigation.showScreenCodes() ? name + ' (' + code + ')' : name;
    }

    selectMenuItem(event: any): void {
        const route = event?.value?.route;
        const parameters = event?.value?.parameters;

        if (!route) {
            return;
        }

        if (event.external) {
            window.open(route, '_blank');
            return;
        }

        this.router
            .navigate([route], { queryParams: parameters })
            .then(() => {
                this.selected = '';
            })
            .catch((error) => {
                console.error('Navigation error:', error);
            });
    }

    private getAllMenuItems() {
        return this._appNavigationService
            .getAllMenuItems()
            .filter((item) => this.showMenuItem(item) && item.route)
            .map((menuItem) => {
                return {
                    name: this.l(menuItem.name),
                    route: menuItem.route,
                    external: menuItem.external,
                    parameters: menuItem.parameters,
                };
            });
    }

    private initializeMenuSearch() {
        this.isMenuSearchActive = false;
        let themeSettings = this.currentTheme.baseSettings;

        if (themeSettings && themeSettings.menu && themeSettings.menu.searchActive) {
            this.allMenuItems = this.getAllMenuItems();
            this.isMenuSearchActive = true;
        }
    }
}
