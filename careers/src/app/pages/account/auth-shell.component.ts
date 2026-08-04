import { Component, inject, input } from '@angular/core';
import { BrandingService } from '@core/branding.service';

/**
 * The narrow, centred card that all four account screens (sign in, register, forgot password, reset
 * password) sit inside, with the tenant's own logo above it.
 */
@Component({
    selector: 'careers-auth-shell',
    template: `
        <div class="auth">
            <div class="auth__card card">
                <div class="auth__brand">
                    @if (branding.logoUrl(); as logo) {
                        <img [src]="logo" [alt]="branding.displayName()" />
                    } @else {
                        <span class="auth__mark">{{ (branding.displayName() || 'C').charAt(0) }}</span>
                    }
                </div>

                <h1 class="auth__title">{{ heading() }}</h1>
                @if (subheading()) {
                    <p class="auth__subtitle muted">{{ subheading() }}</p>
                }

                <ng-content />
            </div>
        </div>
    `,
    styles: [
        `
            .auth {
                display: flex;
                justify-content: center;
                padding: 3rem 1.25rem 4rem;
            }
            .auth__card {
                width: 100%;
                max-width: 30rem;
                padding: 2rem;
            }
            .auth__brand {
                display: flex;
                justify-content: center;
                margin-bottom: 1.25rem;
            }
            .auth__brand img {
                height: 2.75rem;
                max-width: 12rem;
                object-fit: contain;
            }
            .auth__mark {
                display: inline-flex;
                align-items: center;
                justify-content: center;
                width: 2.75rem;
                height: 2.75rem;
                border-radius: 0.75rem;
                background: var(--c-accent);
                color: #fff;
                font-weight: 800;
                font-size: 1.375rem;
                text-transform: uppercase;
            }
            .auth__title {
                font-size: 1.375rem;
                text-align: center;
                margin-bottom: 0.25rem;
            }
            .auth__subtitle {
                text-align: center;
                font-size: 0.9375rem;
                margin-bottom: 1.5rem;
            }
        `,
    ],
})
export class AuthShellComponent {
    readonly branding = inject(BrandingService);

    readonly heading = input.required<string>();
    readonly subheading = input<string>('');
}
