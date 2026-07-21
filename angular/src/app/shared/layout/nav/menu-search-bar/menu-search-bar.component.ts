import { Component, inject } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppNavigationService } from '../app-navigation.service';
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

    searchMenuItem(event) {
        this.searchMenuResults = this.allMenuItems
            .filter(
                (item) =>
                    item.name.toLowerCase().includes(event.query.toLowerCase()) ||
                    item.route.toLowerCase().includes(event.query.toLowerCase())
            )
            .map((menuItem) => {
                return {
                    name: menuItem.name,
                    route: menuItem.route,
                    external: menuItem.external,
                    parameters: menuItem.parameters,
                };
            });
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
