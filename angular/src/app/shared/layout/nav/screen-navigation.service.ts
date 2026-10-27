import { Injectable, inject, signal, computed } from '@angular/core';
import { finalize } from 'rxjs/operators';
import {
    ScreensServiceProxy,
    MyScreenShortcutsServiceProxy,
    ScreenDto,
    ScreenUsageDto,
    SetScreenFavoriteInput,
    RecordScreenVisitInput,
} from '@shared/service-proxies/service-proxies';

/**
 * Screen Codes, favourites, recents and shared quick links — loaded once, read everywhere.
 *
 * WHY ONE SERVICE AND NOT A FETCH PER CONSUMER. Four things need this data: the menu (to suffix codes),
 * the menu search (to match on code), the favourite star, and the dashboard tile. Each fetching for
 * itself would mean four round trips before the shell can draw, on the hottest path in the application,
 * and four copies that disagree the moment one of them stars something.
 *
 * The star and the visit recorder update local state IMMEDIATELY and let the server call follow. A star
 * that waits for a round trip before filling in feels broken, and the failure it guards against —
 * the request losing — is both rare and harmless: the next page load simply shows the truth.
 */
@Injectable({ providedIn: 'root' })
export class ScreenNavigationService {
    private _screensService = inject(ScreensServiceProxy);
    private _shortcutsService = inject(MyScreenShortcutsServiceProxy);

    readonly screens = signal<ScreenDto[]>([]);
    readonly favorites = signal<ScreenUsageDto[]>([]);
    readonly recent = signal<ScreenUsageDto[]>([]);
    readonly sharedQuickLinks = signal<ScreenDto[]>([]);
    readonly showScreenCodes = signal<boolean>(true);
    readonly isLoaded = signal<boolean>(false);

    /** Route -> screen, for turning the router's URL into a screen without a linear scan each time. */
    private readonly byRoute = computed(() => {
        const map = new Map<string, ScreenDto>();
        for (const screen of this.screens()) {
            if (screen.route) {
                map.set(screen.route.toLowerCase(), screen);
            }
        }
        return map;
    });

    private readonly byKey = computed(() => {
        const map = new Map<string, ScreenDto>();
        for (const screen of this.screens()) {
            map.set(screen.screenKey, screen);
        }
        return map;
    });

    private readonly favoriteKeys = computed(() => new Set(this.favorites().map((f) => f.screenKey)));

    /**
     * True while the bootstrap request is on its way. The side menu and the Home quick links both call load() when
     * the page first opens, and both guard on isLoaded() -- which only turns true once the response arrives. So the
     * second caller used to see "not loaded" and send a second request while the first was still in flight. That
     * doubled the work on every first page, and on a new company it made the two requests race to seed the screen
     * catalogue, one of them failing with an "internal error" dialog. (The server now serialises the seeding too;
     * this stops the page asking twice.)
     */
    private _isLoading = false;

    load(): void {
        if (this._isLoading) {
            return;
        }

        this._isLoading = true;

        this._screensService.getNavigationBootstrap().pipe(finalize(() => (this._isLoading = false))).subscribe({
            next: (result) => {
                this.screens.set(result.screens ?? []);
                this.favorites.set(result.favorites ?? []);
                this.recent.set(result.recent ?? []);
                this.sharedQuickLinks.set(result.sharedQuickLinks ?? []);
                this.showScreenCodes.set(result.showScreenCodes !== false);
                this.isLoaded.set(true);
            },
            error: () => {
                // The shell must render regardless. Codes and shortcuts are an aid; failing to fetch
                // them is not a reason to leave somebody without a menu.
                this.isLoaded.set(true);
            },
        });
    }

    screenForRoute(route: string): ScreenDto | undefined {
        if (!route) {
            return undefined;
        }

        // Strip query string and route parameters: the catalogue holds the menu's own path.
        const clean = route.split('?')[0].toLowerCase();
        return this.byRoute().get(clean);
    }

    screenForKey(key: string): ScreenDto | undefined {
        return this.byKey().get(key);
    }

    codeForRoute(route: string): string | undefined {
        return this.screenForRoute(route)?.code;
    }

    isFavorite(screenKey: string): boolean {
        return this.favoriteKeys().has(screenKey);
    }

    /**
     * The screens behind the user's favourites, resolved and in the order the server ranked them.
     *
     * A COMPUTED, not a method. Read from an @for in two templates; as a method it returned a fresh
     * array on every change-detection pass, so @for saw a new identity on every keystroke and rebuilt
     * the list continuously.
     */
    readonly favoriteScreens = computed<ScreenDto[]>(() =>
        this.favorites()
            .map((f) => this.byKey().get(f.screenKey))
            .filter((s): s is ScreenDto => !!s)
    );

    /**
     * Only the screens the user ACTUALLY pinned.
     *
     * The server pads `favorites` with most-visited screens when someone has pinned nothing, so the
     * dashboard tile is not empty on day one. Those arrive with isFavorite false. Now that Recently
     * Visited is a section in its own right that padding would be the same list twice, so anywhere a
     * section is headed "Favorites" it must use this, not favoriteScreens.
     */
    readonly pinnedScreens = computed<ScreenDto[]>(() =>
        this.favorites()
            .filter((f) => f.isFavorite)
            .map((f) => this.byKey().get(f.screenKey))
            .filter((s): s is ScreenDto => !!s)
    );

    /** Recents, minus anything pinned: the same screen in two adjacent lists helps nobody. */
    readonly recentScreens = computed<ScreenDto[]>(() => {
        const pinned = new Set(this.favorites().filter((f) => f.isFavorite).map((f) => f.screenKey));

        return this.recent()
            .filter((r) => !pinned.has(r.screenKey))
            .map((r) => this.byKey().get(r.screenKey))
            .filter((s): s is ScreenDto => !!s);
    });

    toggleFavorite(screen: ScreenDto): void {
        const wasFavorite = this.isFavorite(screen.screenKey);

        // Optimistic: the star fills in on click, not on response.
        if (wasFavorite) {
            this.favorites.set(this.favorites().filter((f) => f.screenKey !== screen.screenKey));
        } else {
            const existing = this.favorites();
            const added = new ScreenUsageDto();
            added.screenKey = screen.screenKey;
            added.isFavorite = true;
            added.visitCount = 0;
            this.favorites.set([...existing, added]);
        }

        const input = new SetScreenFavoriteInput();
        input.screenKey = screen.screenKey;
        input.isFavorite = !wasFavorite;

        this._shortcutsService.setFavorite(input).subscribe({
            error: () => {
                // Put it back. Leaving a star filled in for something the server did not record would
                // be worse than the flicker: the user would believe it was pinned.
                this.favorites.set(
                    wasFavorite
                        ? [...this.favorites(), this.usageFor(screen.screenKey)]
                        : this.favorites().filter((f) => f.screenKey !== screen.screenKey)
                );
            },
        });
    }

    recordVisit(route: string): void {
        const screen = this.screenForRoute(route);
        if (!screen) {
            // Not every route is a catalogued screen — record detail pages, for instance. Recording an
            // unknown key would fill the recents list with things the menu cannot offer back.
            return;
        }

        const input = new RecordScreenVisitInput();
        input.screenKey = screen.screenKey;

        // Fire and forget: a failed visit count is not worth telling anybody about.
        this._shortcutsService.recordVisit(input).subscribe({ error: () => {} });
    }

    private usageFor(screenKey: string): ScreenUsageDto {
        const usage = new ScreenUsageDto();
        usage.screenKey = screenKey;
        usage.isFavorite = true;
        usage.visitCount = 0;
        return usage;
    }
}
