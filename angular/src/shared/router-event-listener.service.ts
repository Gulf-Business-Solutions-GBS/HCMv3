import { Injectable, inject } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { AppUiCustomizationService } from '@shared/common/ui/app-ui-customization.service';

@Injectable({
    providedIn: 'root',
})
export class RouterEventListenerService {
    private router = inject(Router);
    private _uiCustomizationService = inject(AppUiCustomizationService);

    constructor() {
        this.router.events.subscribe((event) => {
            if (event instanceof NavigationEnd) {
                setTimeout(() => {
                    this.toggleBodyCssClass(event.url);
                }, 0);
            }
        });
    }

    private toggleBodyCssClass(url: string): void {
        if (url) {
            if (url === '/') {
                if (abp.session.userId > 0) {
                    this.setAppModuleBodyClassInternal();
                } else {
                    this.setAccountModuleBodyClassInternal();
                }
            }

            if (url.indexOf('/account/') >= 0) {
                this.setAccountModuleBodyClassInternal();
            } else {
                this.setAppModuleBodyClassInternal();
            }
        }
    }

    private setAppModuleBodyClassInternal(): void {
        let currentBodyClass = document.body.className;
        let classesToRemember = '';

        if (currentBodyClass.indexOf('brand-minimize') >= 0) {
            classesToRemember += ' brand-minimize ';
        }
        if (currentBodyClass.indexOf('aside-left-minimize') >= 0) {
            classesToRemember += ' aside-left-minimize';
        }
        if (currentBodyClass.indexOf('brand-hide') >= 0) {
            classesToRemember += ' brand-hide';
        }
        if (currentBodyClass.indexOf('aside-left-hide') >= 0) {
            classesToRemember += ' aside-left-hide';
        }
        if (currentBodyClass.indexOf('swal2-toast-shown') >= 0) {
            classesToRemember += ' swal2-toast-shown';
        }

        document.body.className = this._uiCustomizationService.getAppModuleBodyClass() + ' ' + classesToRemember;
    }

    private setAccountModuleBodyClassInternal(): void {
        let currentBodyClass = document.body.className;
        let classesToRemember = '';

        if (currentBodyClass.indexOf('swal2-toast-shown') >= 0) {
            classesToRemember += ' swal2-toast-shown';
        }

        document.body.className = this._uiCustomizationService.getAccountModuleBodyClass() + ' ' + classesToRemember;
    }
}
