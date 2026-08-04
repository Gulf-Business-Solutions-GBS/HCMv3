import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LocalizePipe } from '@core/localize.pipe';

/** Shared sub-navigation for the three signed-in candidate pages. */
@Component({
    selector: 'careers-candidate-nav',
    imports: [RouterLink, RouterLinkActive, LocalizePipe],
    template: `
        <nav class="subnav">
            <a routerLink="/my/profile" routerLinkActive="is-active">{{ 'CareersMyProfile' | localize }}</a>
            <a routerLink="/my/cvs" routerLinkActive="is-active">{{ 'CareersMyCvs' | localize }}</a>
            <a routerLink="/my/applications" routerLinkActive="is-active">
                {{ 'CareersMyApplications' | localize }}
            </a>
        </nav>
    `,
    styles: [
        `
            .subnav {
                display: flex;
                gap: 0.25rem;
                border-bottom: 1px solid var(--c-line);
                margin-bottom: 1.75rem;
                overflow-x: auto;
            }
            .subnav a {
                padding: 0.75rem 1rem;
                font-weight: 600;
                font-size: 0.9375rem;
                color: var(--c-muted);
                border-bottom: 2px solid transparent;
                white-space: nowrap;
                text-decoration: none;
            }
            .subnav a:hover {
                color: var(--c-ink);
                text-decoration: none;
            }
            .subnav a.is-active {
                color: var(--c-accent);
                border-bottom-color: var(--c-accent);
            }
        `,
    ],
})
export class CandidateNavComponent {}
