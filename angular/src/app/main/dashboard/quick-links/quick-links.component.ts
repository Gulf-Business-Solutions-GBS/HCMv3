import { Component, OnInit, inject, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ScreenNavigationService } from '@app/shared/layout/nav/screen-navigation.service';
import { ScreenDto } from '@shared/service-proxies/service-proxies';

/**
 * Dashboard quick links: the shared band an administrator publishes, then the user's own.
 *
 * TWO BANDS IN ONE TILE, not two tiles. They answer the same question -- "where am I going?" -- and as
 * separate tiles they would compete for the same corner of the dashboard while saying the same thing.
 *
 * The permission filtering that matters happens on the SERVER: a link to a screen the viewer cannot
 * open is absent from the response entirely, so this component never has to decide what to hide. That
 * is deliberate -- a client-side filter would still have shipped the screen's name to the browser, and
 * "Termination Processing" appearing in a network response tells an ordinary user something they were
 * not meant to know.
 */
@Component({
    selector: 'quick-links',
    templateUrl: './quick-links.component.html',
    styleUrl: './quick-links.component.css',
    imports: [RouterLink, LocalizePipe],
})
export class QuickLinksComponent extends AppComponentBase implements OnInit {
    private _screenNavigation = inject(ScreenNavigationService);

    /** Published by an administrator for the whole tenant. */
    readonly shared = computed<ScreenDto[]>(() => this._screenNavigation.sharedQuickLinks());

    /**
     * Only what the user actually starred -- NOT the server's most-visited padding. With Recently
     * Visited now a section of its own, that padding would print the same screens twice under two
     * different headings, one of which would be lying about how they got there.
     */
    readonly favorites = computed<ScreenDto[]>(() => this._screenNavigation.pinnedScreens());

    /** Recents, already excluding anything starred above. */
    readonly recent = computed<ScreenDto[]>(() => this._screenNavigation.recentScreens());

    readonly showScreenCodes = computed<boolean>(() => this._screenNavigation.showScreenCodes());

    readonly isEmpty = computed<boolean>(
        () => this.shared().length === 0 && this.favorites().length === 0 && this.recent().length === 0
    );

    ngOnInit(): void {
        // The sidebar normally loads this first; the guard is for a dashboard reached before it renders.
        if (!this._screenNavigation.isLoaded()) {
            this._screenNavigation.load();
        }
    }
}
