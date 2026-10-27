import { Component, inject } from '@angular/core';
import { BrandingService } from '@core/branding.service';
import { LocalizationService } from '@core/localization.service';

@Component({
    selector: 'careers-footer',
    template: `
        <footer class="site-footer">
            <div class="container site-footer__inner">
                <p class="small muted">{{ copyright }}</p>
            </div>
        </footer>
    `,
    styles: [
        `
            .site-footer {
                border-top: 1px solid var(--c-line);
                background: var(--c-surface);
                padding: 1.5rem 0;
                margin-top: auto;
            }
            .site-footer__inner p {
                margin: 0;
            }
        `,
    ],
})
export class FooterComponent {
    private _branding = inject(BrandingService);
    private _localization = inject(LocalizationService);

    get copyright(): string {
        const owner = `${new Date().getFullYear()} ${this._branding.displayName()}`.trim();
        return this._localization.localize('CareersAllRightsReserved', owner);
    }
}
