import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '@core/auth.service';
import { BrandingService } from '@core/branding.service';
import { LocalizePipe } from '@core/localize.pipe';

@Component({
    selector: 'careers-header',
    imports: [RouterLink, RouterLinkActive, LocalizePipe],
    templateUrl: './header.component.html',
    styleUrl: './header.component.css',
})
export class HeaderComponent {
    readonly branding = inject(BrandingService);
    readonly auth = inject(AuthService);
    private _router = inject(Router);

    /** Mobile navigation drawer state. */
    readonly menuOpen = signal(false);

    toggleMenu(): void {
        this.menuOpen.update((open) => !open);
    }

    closeMenu(): void {
        this.menuOpen.set(false);
    }

    logout(): void {
        this.auth.logout();
        this.closeMenu();
        void this._router.navigate(['/']);
    }
}
