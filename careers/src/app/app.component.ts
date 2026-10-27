import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { BrandingService } from '@core/branding.service';
import { ToastComponent } from '@core/toast.component';
import { LocalizePipe } from '@core/localize.pipe';
import { HeaderComponent } from '@layout/header.component';
import { FooterComponent } from '@layout/footer.component';

@Component({
    selector: 'careers-root',
    imports: [RouterOutlet, HeaderComponent, FooterComponent, ToastComponent, LocalizePipe],
    template: `
        <careers-header />

        <main id="content">
            @if (branding.loadFailed()) {
                <!-- The backend fails closed when the host name does not match App:CareersDomainFormat.
                     Say so plainly instead of rendering an empty site. -->
                <div class="container page">
                    <div class="alert alert--error">{{ 'CareersTenantNotResolved' | localize }}</div>
                </div>
            } @else {
                <router-outlet />
            }
        </main>

        <careers-footer />
        <careers-toasts />
    `,
    styles: [
        `
            :host {
                display: flex;
                flex-direction: column;
                min-height: 100vh;
            }
            main {
                flex: 1;
            }
        `,
    ],
})
export class AppComponent implements OnInit {
    readonly branding = inject(BrandingService);

    ngOnInit(): void {
        // One fetch for the whole session; the header and footer read the same cached signal.
        void this.branding.load();
    }
}
