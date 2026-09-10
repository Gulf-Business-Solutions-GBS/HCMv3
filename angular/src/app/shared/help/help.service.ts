import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

/** One documented field. Shape is produced by tools/help/help-tool.mjs build. */
export interface HelpField {
    id: string;
    label: string;
    section: string;
    aliases: string[];
    rules: string | null;
    when: string | null;
    html: string;
    adminHtml: string;
    stub: boolean;
}

export interface HelpPageContent {
    helpId: string;
    lang: string;
    title: string;
    entity: string;
    overviewHtml: string;
    aliasIndex: Record<string, string>;
    fields: HelpField[];
}

interface RegisteredPage {
    helpId: string;
    host: HTMLElement;
}

/**
 * Owns the help drawer's state and, more importantly, the mapping from "what the user was
 * pointing at" to "which topic that is".
 *
 * The resolver reads the DOM rather than a registry, which is what lets the same content serve a
 * read-only card screen and an edit form with no per-screen wiring: the read view marks its labels
 * with `data-help-loc="<localization key>"`, the edit form already carries `id="Entity_Property"`
 * from the ASP.NET Zero conventions, and both are declared as aliases in the compiled JSON.
 *
 * Reading the live DOM is also what implements the visibility rule (DESIGN.md §8.1): a field hidden
 * by @if, by a permission or by a feature check is simply not there to be found, so its help is
 * never shown. No second copy of the entitlement rules.
 */
@Injectable({ providedIn: 'root' })
export class HelpService {
    private _http = inject(HttpClient);
    private _cache = new Map<string, HelpPageContent>();

    readonly isOpen = signal(false);
    readonly loading = signal(false);
    readonly failed = signal(false);
    readonly content = signal<HelpPageContent | null>(null);
    readonly activeFieldId = signal<string | null>(null);

    /** Ids present in the host screen's DOM right now; null means "show everything". */
    readonly visibleIds = signal<Set<string> | null>(null);

    private _page: RegisteredPage | null = null;
    private _lastHover: Element | null = null;
    private _pendingToken: string | null = null;

    constructor() {
        document.addEventListener('mouseover', this._onMouseOver, true);
        document.addEventListener('keydown', this._onKeyDown, true);
    }

    // -- page registration ---------------------------------------------------------------------

    registerPage(helpId: string, host: HTMLElement): void {
        this._page = { helpId, host };
    }

    unregisterPage(host: HTMLElement): void {
        if (this._page?.host === host) {
            this._page = null;
            this.close();
        }
    }

    hasPage(): boolean {
        return this._page !== null;
    }

    // -- opening -------------------------------------------------------------------------------

    openPage(): void {
        this.activeFieldId.set(null);
        void this._open();
    }

    /**
     * `token` is a raw DOM marker (`loc:Nationality`, `dom:Employee_Code`) or an explicit canonical
     * id -- never something that needed the alias index to produce. The index only exists after the
     * content request finishes, and the first F1 on a screen necessarily happens before that, so
     * resolving at capture time silently targeted nothing on exactly the press that matters most.
     */
    openField(token: string | null): void {
        this._pendingToken = token;
        this.activeFieldId.set(token ? this._resolveToken(token) : null);
        void this._open();
    }

    private _resolveToken(token: string): string | null {
        const index = this.content()?.aliasIndex;
        if (!index) return null;
        return index[token] ?? (token.includes(':') ? null : token);
    }

    close(): void {
        this.isOpen.set(false);
    }

    toggle(): void {
        this.isOpen() ? this.close() : this.openPage();
    }

    private async _open(): Promise<void> {
        if (!this._page) return;
        this.isOpen.set(true);
        this._snapshotVisibleFields();
        await this._load(this._page.helpId);

        // The index exists now, so a marker captured before the fetch can finally be named.
        if (this._pendingToken) {
            this.activeFieldId.set(this._resolveToken(this._pendingToken));
            this._pendingToken = null;
        }
        this._snapshotVisibleFields();
    }

    // -- content loading -----------------------------------------------------------------------

    /** "en-GB" and "en" both resolve to the en content set; anything unknown falls back to en. */
    private _lang(): string {
        const name: string = (window as any).abp?.localization?.currentLanguage?.name || 'en';
        return name.split('-')[0].toLowerCase();
    }

    private async _load(helpId: string): Promise<void> {
        const lang = this._lang();
        const key = `${lang}/${helpId}`;
        const cached = this._cache.get(key);
        if (cached) {
            this.content.set(cached);
            this.failed.set(false);
            return;
        }

        this.loading.set(true);
        this.failed.set(false);
        try {
            let doc = await this._fetch(lang, helpId);
            if (!doc && lang !== 'en') doc = await this._fetch('en', helpId);
            if (!doc) {
                this.failed.set(true);
                this.content.set(null);
                return;
            }
            this._cache.set(key, doc);
            this.content.set(doc);
        } finally {
            this.loading.set(false);
        }
    }

    private _fetch(lang: string, helpId: string): Promise<HelpPageContent | null> {
        const url = `assets/help/${lang}/${helpId}.json`;
        return new Promise((resolve) => {
            this._http.get<HelpPageContent>(url).subscribe({
                next: (doc) => resolve(doc),
                error: () => resolve(null),
            });
        });
    }

    // -- visibility (DESIGN.md §8.1) -----------------------------------------------------------

    /**
     * Which topics the host screen is actually showing. Falls back to null -- meaning "no opinion,
     * render everything" -- when the screen carries no markers at all, so a page that has not been
     * annotated still gets its full help rather than an empty drawer.
     */
    private _snapshotVisibleFields(): void {
        const page = this._page;
        const doc = this.content();
        if (!page) {
            this.visibleIds.set(null);
            return;
        }
        const index = doc?.aliasIndex;
        const found = new Set<string>();

        page.host.querySelectorAll('[data-help-loc]').forEach((el) => {
            const loc = el.getAttribute('data-help-loc');
            const id = index?.[`loc:${loc}`];
            if (id) found.add(id);
            else if (loc) found.add(`loc:${loc}`); // resolved once content arrives
        });
        page.host.querySelectorAll('[id]').forEach((el) => {
            const id = index?.[`dom:${(el as HTMLElement).id}`];
            if (id) found.add(id);
        });

        this.visibleIds.set(found.size ? found : null);
    }

    // -- DOM resolution ------------------------------------------------------------------------

    private _onMouseOver = (e: Event): void => {
        this._lastHover = e.target as Element;
    };

    private _onKeyDown = (e: KeyboardEvent): void => {
        if (e.key === 'Escape' && this.isOpen()) {
            e.preventDefault();
            e.stopPropagation();
            this.close();
            return;
        }
        const isHelpKey = e.key === 'F1' || (e.altKey && (e.key === 'h' || e.key === 'H'));
        if (!isHelpKey || !this._page) return;

        // F1 otherwise opens the browser's own help in Chrome and Edge.
        e.preventDefault();
        e.stopPropagation();
        this.openField(this._resolveTarget());
    };

    private _resolveTarget(): string | null {
        const page = this._page;
        if (!page) return null;

        const active = document.activeElement;
        const start =
            active && active !== document.body && page.host.contains(active)
                ? active
                : this._lastHover && page.host.contains(this._lastHover)
                  ? this._lastHover
                  : null;

        return start ? this._tokenFor(start) : null;
    }

    /**
     * Walk outwards from the pointed-at element. A description list is the awkward case: the
     * marker sits on the `<dt>` but the pointer is usually over the `<dd>` beside it, which is a
     * sibling rather than a descendant.
     */
    private _tokenFor(el: Element): string | null {
        let node: Element | null = el;

        while (node && node !== document.body) {
            const direct = this._markerOn(node);
            if (direct) return direct;

            if (node.tagName === 'DD') {
                let prev = node.previousElementSibling;
                while (prev && prev.tagName !== 'DT') prev = prev.previousElementSibling;
                if (prev) {
                    const viaDt = this._markerOn(prev);
                    if (viaDt) return viaDt;
                }
            }
            node = node.parentElement;
        }
        return null;
    }

    /** Reads markers only -- no index lookup, so this works before content has loaded. */
    private _markerOn(node: Element): string | null {
        const loc = node.getAttribute('data-help-loc');
        if (loc) return `loc:${loc}`;

        const explicit = node.getAttribute('data-help');
        if (explicit) return explicit;

        const domId = (node as HTMLElement).id;
        // Only ids shaped like the ASP.NET Zero convention; every screen is full of other ids.
        if (domId && /^[A-Za-z][A-Za-z0-9]*_[A-Za-z][A-Za-z0-9]*$/.test(domId)) return `dom:${domId}`;

        return null;
    }
}
