import { Component, inject, input } from '@angular/core';
import { NavigationExtras, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';

export class BreadcrumbItem {
    text: string;
    routerLink?: string;
    navigationExtras?: NavigationExtras;

    constructor(text: string, routerLink?: string, navigationExtras?: NavigationExtras) {
        this.text = text;
        this.routerLink = routerLink;
        this.navigationExtras = navigationExtras;
    }

    isLink(): boolean {
        return !!this.routerLink;
    }
}

@Component({
    selector: 'sub-header',
    templateUrl: './sub-header.component.html',
    imports: [],
})
export class SubHeaderComponent extends AppComponentBase {
    private _router = inject(Router);

    readonly title = input<string>(undefined);
    readonly description = input<string>(undefined);
    readonly breadcrumbs = input<BreadcrumbItem[]>(undefined);

    goToBreadcrumb(breadcrumb: BreadcrumbItem): void {
        if (!breadcrumb.routerLink) {
            return;
        }

        if (breadcrumb.navigationExtras) {
            this._router.navigate([breadcrumb.routerLink], breadcrumb.navigationExtras);
        } else {
            this._router.navigate([breadcrumb.routerLink]);
        }
    }
}
