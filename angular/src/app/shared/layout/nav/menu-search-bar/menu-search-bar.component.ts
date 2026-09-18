import { Component, inject, output, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AppNavigationService } from '../app-navigation.service';
import { ScreenNavigationService } from '../screen-navigation.service';
import { Router } from '@angular/router';

import { AutoCompleteModule } from 'primeng/autocomplete';
import { FormsModule } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AppConsts } from '@shared/AppConsts';
import {
    GlobalSearchRecentItemDto,
    GlobalSearchRecentItemInput,
    GlobalSearchServiceProxy,
    SearchPeopleInput,
} from '@shared/service-proxies/service-proxies';

/** One row in the dropdown. `kind` decides which template renders it and what selecting it does. */
interface GlobalSearchResult {
    kind: 'action' | 'person';
    /** Shown under "Recent", with a remove button. */
    recent?: boolean;
    name: string;
    /** Job title and department, for a person. */
    subtitle?: string;
    /** Action only. */
    route?: string;
    external?: boolean;
    parameters?: any;
    /** Person only -- the employee id the route is built from. */
    employeeId?: number;
    photoUrl?: string | null;
}

interface GlobalSearchGroup {
    label: string;
    items: GlobalSearchResult[];
    /** The "Recent" group, whose heading carries a "Clear recent" link. */
    recent?: boolean;
}

/**
 * The header's "search for actions or people" bar.
 *
 * Two sources in one field. ACTIONS are the menu the client already holds, so they are filtered locally
 * and appear instantly -- no round trip to learn that "business rules" is a screen. PEOPLE come from
 * GlobalSearchAppService, which any signed-in user of the tenant may call.
 *
 * The two are grouped rather than interleaved: a screen and a colleague are different kinds of answer,
 * and ranking them against each other would mean inventing a relevance score that satisfies neither.
 */
@Component({
    selector: 'menu-search-bar',
    templateUrl: './menu-search-bar.component.html',
    styleUrls: ['./menu-search-bar.component.css'],
    imports: [AutoCompleteModule, FormsModule, LocalizePipe],
})
export class MenuSearchBarComponent extends AppComponentBase {
    /** Matches the org-chart cards, so a face fetched for one is already cached for the other. */
    private static readonly PhotoSize = 112;

    /** Enough to be worth reading; more would bury the action results in a shared dropdown. */
    private static readonly PeopleLimit = 8;

    private _appNavigationService = inject(AppNavigationService);
    private _screenNavigation = inject(ScreenNavigationService);
    private _globalSearchService = inject(GlobalSearchServiceProxy);
    private router = inject(Router);

    allMenuItems: any[];

    /**
     * A signal, not a plain field, because the app runs zoneless. People arrive over HTTP after the
     * keystroke that asked for them, and assigning a plain field from that callback schedules no change
     * detection -- the panel went on showing "No results found" until the next key press repainted it.
     */
    readonly searchResults = signal<GlobalSearchGroup[]>([]);

    isMenuSearchActive = false;

    /**
     * The field's own value: PrimeNG writes the chosen row here on select, and clearing it empties the
     * box. A signal for the same reason as searchResults -- an action clears it from inside
     * router.navigate().then(), long after the click Angular was watching, so a plain field left the
     * chosen screen's name sitting in the box after navigating away.
     */
    readonly selected = signal<string | GlobalSearchResult>('');

    /** Off while the empty field shows the recent list, so a user with no history sees nothing, not "No results found". */
    readonly showEmptyMessage = signal(true);

    /**
     * Fires as soon as a result is chosen, so a host that opened this in an overlay -- the mobile header
     * does -- can close itself. Emitted before navigation rather than after, because the overlay should
     * come down on the tap, not once the target route has finished resolving.
     */
    readonly resultSelected = output<void>();

    constructor() {
        super();
        this.initializeMenuSearch();
    }

    showMenuItem(menuItem): boolean {
        return this._appNavigationService.showMenuItem(menuItem);
    }

    /**
     * Actions are resolved synchronously and shown at once; people arrive when the server answers and are
     * merged in. Doing it this way means the dropdown never waits on the network to show a screen match,
     * which is the common case and the one that has to feel instant.
     */
    search(event: { query: string }): void {
        const query = (event.query || '').trim();

        // An empty box -- the field was just focused -- shows what this user opened last instead.
        if (!query) {
            this.showEmptyMessage.set(false);
            this.showRecent();
            return;
        }

        this.showEmptyMessage.set(true);

        const actions = this.searchActions(query);
        this.searchResults.set(this.buildGroups(actions, []));

        this._globalSearchService.searchPeople(this.buildPeopleInput(query)).subscribe({
            next: (result) => {
                const people = (result.people ?? []).map((person) => this.toPersonResult(person));
                this.searchResults.set(this.buildGroups(actions, people));
            },
            // A failed people lookup must not blank the action results that already rendered.
            error: () => {
                this.searchResults.set(this.buildGroups(actions, []));
            },
        });
    }

    selectResult(event: any): void {
        const item: GlobalSearchResult = event?.value;

        if (!item) {
            return;
        }

        this.resultSelected.emit();

        if (item.kind === 'person') {
            this.selected.set('');
            this.remember(item);
            this.navigate(`/app/admin/employees/${item.employeeId}`, undefined, false);
            return;
        }

        if (!item.route) {
            return;
        }

        this.remember(item);
        this.navigate(item.route, item.parameters, item.external);
    }

    /**
     * The ✕ on a recent row. Stops the click reaching the row, which would otherwise open the result it is
     * removing.
     */
    removeRecent(event: Event, item: GlobalSearchResult): void {
        event.preventDefault();
        event.stopPropagation();

        this._globalSearchService.removeRecentSearch(this.toRecentInput(item)).subscribe(() => this.showRecent());
    }

    clearRecent(event: Event): void {
        event.preventDefault();
        event.stopPropagation();

        this._globalSearchService.clearRecentSearches().subscribe(() => this.searchResults.set([]));
    }

    /**
     * The user's last opened results, from the server each time the empty field is focused -- so a list
     * changed in another tab or on another machine is current. A screen the user can no longer open is
     * left out: only routes in the menu built from their permissions are shown.
     */
    private showRecent(): void {
        this._globalSearchService.getRecentSearches().subscribe({
            next: (result) => {
                const items = (result.items ?? [])
                    .map((entry) => this.toRecentResult(entry))
                    .filter((entry): entry is GlobalSearchResult => !!entry);

                this.searchResults.set(
                    items.length ? [{ label: this.l('RecentSearches'), items, recent: true }] : []
                );
            },
            // The recent list is a convenience; failing to load it must not show an error in the header.
            error: () => this.searchResults.set([]),
        });
    }

    private toRecentResult(entry: GlobalSearchRecentItemDto): GlobalSearchResult | null {
        if (entry.kind === 'person' && entry.person) {
            return { ...this.toPersonResult(entry.person), recent: true };
        }

        const menuItem = this.allMenuItems?.find((item) => item.route === entry.route);
        if (!menuItem) {
            return null;
        }

        return {
            kind: 'action',
            recent: true,
            name: this.labelFor(menuItem.name, this._screenNavigation.codeForRoute(menuItem.route)),
            route: menuItem.route,
            external: menuItem.external,
            parameters: menuItem.parameters,
        };
    }

    /** Saved in the background; the user is already on their way, so a failure is not worth telling them about. */
    private remember(item: GlobalSearchResult): void {
        this._globalSearchService.addRecentSearch(this.toRecentInput(item)).subscribe({ error: () => undefined });
    }

    private toRecentInput(item: GlobalSearchResult): GlobalSearchRecentItemInput {
        const input = new GlobalSearchRecentItemInput();
        input.kind = item.kind;
        if (item.kind === 'person') {
            input.employeeId = item.employeeId;
        } else {
            input.route = item.route;
        }
        return input;
    }

    /**
     * Falls back to the blank avatar when the photo endpoint 404s, which it does for anyone without one.
     *
     * Replaces the row through the signal rather than mutating it in place: zoneless again -- an image
     * error fires outside any handler Angular is watching, so a mutated field would leave the broken
     * image on screen until something else happened to repaint.
     */
    onPhotoError(result: GlobalSearchResult): void {
        this.searchResults.update((groups) =>
            groups.map((group) => ({
                ...group,
                items: group.items.map((item) => (item === result ? { ...item, photoUrl: null } : item)),
            }))
        );
    }

    /**
     * Matches on name, route AND Screen Code, in the SAME box -- no second search field. Somebody who
     * knows a screen is SPY007 should be able to type it where they already type its name, and somebody
     * who does not should never notice codes exist here.
     */
    private searchActions(query: string): GlobalSearchResult[] {
        const lowered = query.toLowerCase();

        return this.allMenuItems
            .map((item) => ({ ...item, code: this._screenNavigation.codeForRoute(item.route) }))
            .filter(
                (item) =>
                    item.name.toLowerCase().includes(lowered) ||
                    item.route.toLowerCase().includes(lowered) ||
                    (item.code && item.code.toLowerCase().includes(lowered))
            )
            .map((menuItem) => ({
                kind: 'action' as const,
                // The code rides in the label so a result is identifiable at a glance and the person
                // learns the code of a screen they reached by name.
                name: this.labelFor(menuItem.name, menuItem.code),
                route: menuItem.route,
                external: menuItem.external,
                parameters: menuItem.parameters,
            }));
    }

    private buildPeopleInput(query: string): SearchPeopleInput {
        const input = new SearchPeopleInput();
        input.filter = query;
        input.maxResultCount = MenuSearchBarComponent.PeopleLimit;
        return input;
    }

    private toPersonResult(person: any): GlobalSearchResult {
        const name = this.isRtl ? person.fullNameAr || person.fullNameEn : person.fullNameEn;
        const jobTitle = this.isRtl ? person.jobTitleAr || person.jobTitleEn : person.jobTitleEn;
        const title = [person.code, name].filter((part) => !!part).join(' - ');

        return {
            kind: 'person',
            // "E0012 - Ahmed Ali", then job title and work email beneath it.
            name: title,
            // Both when known, whichever exists otherwise, and nothing at all rather than a stray dash.
            subtitle: [jobTitle, person.email].filter((part) => !!part).join(' — ') || undefined,
            employeeId: person.id,
            photoUrl: person.hasPhoto
                ? `${AppConsts.remoteServiceBaseUrl}/api/employee-photo/${person.id}?size=${MenuSearchBarComponent.PhotoSize}`
                : null,
        };
    }

    /** Empty groups are dropped so the dropdown never shows a heading with nothing under it. */
    private buildGroups(actions: GlobalSearchResult[], people: GlobalSearchResult[]): GlobalSearchGroup[] {
        const groups: GlobalSearchGroup[] = [];

        if (actions.length) {
            groups.push({ label: this.l('Actions'), items: actions });
        }

        if (people.length) {
            groups.push({ label: this.l('People'), items: people });
        }

        return groups;
    }

    private get isRtl(): boolean {
        return abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    }

    private navigate(route: string, parameters: any, external: boolean): void {
        // item.external, NOT event.external. The event is PrimeNG's { originalEvent, value } wrapper and
        // has no such property, so this branch never fired and an external link was handed to the
        // router as an internal path -- where it matched nothing and silently did nothing.
        if (external) {
            window.open(route, '_blank');
            this.selected.set('');
            return;
        }

        this.router
            .navigate([route], { queryParams: parameters })
            .then((navigated) => {
                this.selected.set('');

                if (!navigated) {
                    // navigate() resolves FALSE when a guard blocks -- it does not reject. AppRouteGuard
                    // redirects a denied user to the "best route", which is usually the dashboard, so
                    // somebody already on the dashboard sees the overlay close and nothing else happen.
                    // Saying so turns an invisible refusal into an explanation.
                    this.message.warn(this.l('ScreenNotPermittedOrUnavailable'));
                }
            })
            .catch((error) => {
                // A genuine routing failure -- an unmatched path, or a chunk that would not load.
                console.error('Navigation error:', error);
                this.selected.set('');
                this.message.warn(this.l('ScreenNotPermittedOrUnavailable'));
            });
    }

    private labelFor(name: string, code: string | undefined): string {
        return code && this._screenNavigation.showScreenCodes() ? name + ' (' + code + ')' : name;
    }

    private getAllMenuItems() {
        return this._appNavigationService
            .getAllMenuItems()
            .filter((item) => this.showMenuItem(item) && item.route)
            .map((menuItem) => {
                return {
                    name: this.l(menuItem.name),
                    route: menuItem.route,
                    external: menuItem.external,
                    parameters: menuItem.parameters,
                };
            });
    }

    private initializeMenuSearch() {
        this.isMenuSearchActive = false;
        let themeSettings = this.currentTheme.baseSettings;

        if (themeSettings && themeSettings.menu && themeSettings.menu.searchActive) {
            this.allMenuItems = this.getAllMenuItems();
            this.isMenuSearchActive = true;
        }
    }
}
