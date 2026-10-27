import { Injectable, inject } from '@angular/core';
import { Router, NavigationStart, NavigationEnd } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';

@Injectable({
    providedIn: 'root',
})
export class RouterSpinnerService {
    private router = inject(Router);
    private spinnerService = inject(NgxSpinnerService);

    constructor() {
        const spinnerService = this.spinnerService;

        this.router.events.subscribe((event) => {
            if (event instanceof NavigationStart) {
                this.spinnerService.show();
            }
            if (event instanceof NavigationEnd) {
                const metaTag = document.querySelector('meta[property="og:url"]');
                if (metaTag) {
                    metaTag.setAttribute('content', window.location.href);
                }
                spinnerService.hide();
                // Scroll to top on navigation
                window.scroll(0, 0);
            }
        });
    }
}
