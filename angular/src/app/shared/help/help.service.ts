import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PermissionCheckerService } from 'abp-ng2-module';
import {
    EntitySchemaDto,
    HelpMetadataServiceProxy,
    PropertySchemaDto,
} from '@shared/service-proxies/service-proxies';

/** Must match AppPermissions.Pages_Administration_HelpTechnicalInfo. */
const TECHNICAL_INFO_PERMISSION = 'Pages.Administration.HelpTechnicalInfo';

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

/** `angular/src/assets/help/index.json`, written by tools/help/help-tool.mjs build. */
interface DomIndex {
    dom: Record<string, string[]>;
    /** Edit-dialog component selector (lower case) -> the page documenting that screen. */
    hosts?: Record<string, string>;
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

    /**
     * The open dialog the drawer is currently speaking for, when there is one. A modal is not a
     * registered page -- it is recognised by the fields it is showing (see _helpIdFor) -- so a
     * create/edit form gets its own help with no per-screen wiring, exactly as the read views do.
     */
    private _modalPage: RegisteredPage | null = null;

    /**
     * The screen behind the drawer when it registered no [helpPage]: recognised by the edit dialog it
     * hosts (see _screenPageFor). Found again on every press, as the reader may have moved on.
     */
    private _screenPage: RegisteredPage | null = null;
    private _lastHover: Element | null = null;
    private _pendingToken: string | null = null;
    private _pendingTechnical = false;
    private _domIndex: Record<string, string[]> | null = null;
    private _hostIndex: Record<string, string> = {};
    private _domIndexLoad: Promise<void> | null = null;

    // -- technical information (DESIGN.md §9) --------------------------------------------------

    private _permission = inject(PermissionCheckerService);
    private _metadata = inject(HelpMetadataServiceProxy);
    private _schemaCache = new Map<string, EntitySchemaDto>();

    /** Storage schema of the current page's entity; loaded only when someone asks for it. */
    readonly schema = signal<EntitySchemaDto | null>(null);
    readonly schemaFailed = signal(false);

    /** Topics whose technical panel is open. */
    readonly expandedTechnical = signal<ReadonlySet<string>>(new Set());

    constructor() {
        document.addEventListener('mouseover', this._onMouseOver, true);
        document.addEventListener('keydown', this._onKeyDown, true);
    }

    // -- page registration ---------------------------------------------------------------------

    registerPage(helpId: string, host: HTMLElement): void {
        if (this._page?.helpId !== helpId) {
            // Open technical panels belong to the previous screen's fields.
            this.expandedTechnical.set(new Set());
        }
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

    /** What the drawer is about right now: the dialog in front if it is documented, else the screen. */
    private get _active(): RegisteredPage | null {
        return this._modalPage ?? this._page ?? this._screenPage;
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
        // The dialog it was speaking for may be gone by the next press; recognise it again then.
        this._modalPage = null;
        this._screenPage = null;
    }

    toggle(): void {
        this.isOpen() ? this.close() : this.openPage();
    }

    private async _open(): Promise<void> {
        await this._syncModalScope();
        const page = this._active;
        if (!page) return;
        this.isOpen.set(true);
        this._snapshotVisibleFields();
        await this._load(page.helpId);

        // The index exists now, so a marker captured before the fetch can finally be named.
        if (this._pendingToken) {
            this.activeFieldId.set(this._resolveToken(this._pendingToken));
            this._pendingToken = null;
        }
        // Ctrl+F1: the same topic, with its technical panel already open.
        const target = this.activeFieldId();
        if (this._pendingTechnical && target && !this.expandedTechnical().has(target)) {
            this.toggleTechnical(target);
        }
        this._pendingTechnical = false;
        this._snapshotVisibleFields();
    }

    // -- technical information -----------------------------------------------------------------

    /**
     * Whether to offer the technical panel at all. Checked on the client only to avoid showing a
     * control that would fail -- the server enforces the same permission on every call, which is what
     * actually keeps schema away from people without it.
     */
    canSeeTechnical(): boolean {
        return this._permission.isGranted(TECHNICAL_INFO_PERMISSION);
    }

    isTechnicalOpen(fieldId: string): boolean {
        return this.expandedTechnical().has(fieldId);
    }

    toggleTechnical(fieldId: string): void {
        const next = new Set(this.expandedTechnical());
        if (next.has(fieldId)) next.delete(fieldId);
        else next.add(fieldId);
        this.expandedTechnical.set(next);
        if (next.size) void this._loadSchema();
    }

    /** `Employee.NationalityId` -> the NationalityId column, once the schema has arrived. */
    propertySchema(fieldId: string): PropertySchemaDto | undefined {
        const schema = this.schema();
        if (!schema || schema.entityFullName !== this.content()?.entity) return undefined;
        const property = fieldId.slice(fieldId.indexOf('.') + 1);
        return schema.properties?.find((p) => p.propertyName === property);
    }

    /** One request per entity per session, and only for someone who opened a technical panel. */
    private async _loadSchema(): Promise<void> {
        const entity = this.content()?.entity;
        if (!entity || !this.canSeeTechnical()) return;

        const cached = this._schemaCache.get(entity);
        if (cached) {
            this.schema.set(cached);
            return;
        }

        this.schemaFailed.set(false);
        const schema = await new Promise<EntitySchemaDto | null>((resolve) => {
            this._metadata.getEntitySchema(entity).subscribe({
                next: (s) => resolve(s),
                error: () => resolve(null),
            });
        });
        if (!schema) {
            this.schemaFailed.set(true);
            return;
        }
        this._schemaCache.set(entity, schema);
        this.schema.set(schema);
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
        const page = this._active;
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
        if (!isHelpKey) return;

        // A dialog in front answers for itself; otherwise the screen behind it does.
        const scope = this._modalInFront() ?? this._page?.host ?? null;
        if (!scope) {
            // An unannotated screen. F1 is never wanted for the browser's own help, so take it either
            // way, then look for the screen's edit dialog; with none documented, nothing opens.
            e.preventDefault();
            e.stopPropagation();
            void this._openForScreen();
            return;
        }

        // Ctrl+F1 asks for the technical panel; without the permission it is plain F1, not a refusal.
        this._pendingTechnical = e.key === 'F1' && e.ctrlKey && this.canSeeTechnical();

        // F1 otherwise opens the browser's own help in Chrome and Edge.
        e.preventDefault();
        e.stopPropagation();
        this.openField(this._resolveTarget(scope));
    };

    private async _openForScreen(): Promise<void> {
        await this._ensureDomIndex();
        const page = this._screenPageFor();
        if (page?.helpId !== this._screenPage?.helpId) this.expandedTechnical.set(new Set());
        this._screenPage = page;
        if (page) this.openPage();
    }

    /**
     * The first documented edit dialog hosted on the screen. Its host element stays in the DOM while
     * the dialog is closed, and a screen hosts only its own (the employee record renders one section at
     * a time), so finding any one is enough.
     */
    private _screenPageFor(): RegisteredPage | null {
        for (const [selector, helpId] of Object.entries(this._hostIndex)) {
            const host = document.querySelector<HTMLElement>(selector);
            if (host) return { helpId, host: host.parentElement ?? host };
        }
        return null;
    }

    private _resolveTarget(scope: HTMLElement): string | null {
        const start = this._startElement();
        return start && scope.contains(start) ? this._tokenFor(start) : null;
    }

    /** What the user was pointing at: the focused control, or failing that whatever is under the pointer. */
    private _startElement(): Element | null {
        const active = document.activeElement;
        if (active && active !== document.body) return active;
        return this._lastHover;
    }

    // -- dialogs -------------------------------------------------------------------------------

    /**
     * The dialog the user is in, or the topmost one if the focus has wandered. Bootstrap leaves an
     * open dialog as `.modal.show` and AppBsModalDirective stacks them by z-index, so the one in
     * front is the one with the highest.
     */
    private _modalInFront(): HTMLElement | null {
        const start = this._startElement();
        const own = start?.closest?.('.modal.show') as HTMLElement | null | undefined;
        if (own) return own;

        const open = Array.from(document.querySelectorAll<HTMLElement>('.modal.show'));
        if (!open.length) return null;
        return open.reduce((a, b) => (Number(b.style.zIndex || 0) >= Number(a.style.zIndex || 0) ? b : a));
    }

    /**
     * Works out which page documents the dialog in front, and makes that the drawer's subject.
     *
     * Dialogs are recognised rather than registered: a create/edit form already carries
     * `id="Entity_Property"` on every input from the ASP.NET Zero conventions, and those same ids
     * are aliases in the compiled help, so an index built at compile time can name the page with
     * no markup added to 160-odd modal templates -- and no chance of one being forgotten.
     */
    private async _syncModalScope(): Promise<void> {
        const modal = this._modalInFront();
        if (!modal) {
            this._modalPage = null;
            return;
        }
        if (this._modalPage?.host === modal) return;

        await this._ensureDomIndex();
        const helpId = this._helpIdFor(modal);
        // Open technical panels belong to the fields of whatever the drawer was showing before.
        if (this._modalPage?.helpId !== helpId) this.expandedTechnical.set(new Set());
        // An undocumented dialog falls back to the screen behind it rather than to an empty drawer.
        this._modalPage = helpId ? { helpId, host: modal } : null;
    }

    /**
     * Scores the candidate pages against the ids actually present, rather than taking the first
     * page that matches one: the shared topic pool means `Code` and `NameEn` belong to dozens of
     * pages, and only the weight of a dialog's whole field set separates them.
     */
    private _helpIdFor(scope: HTMLElement): string | null {
        const index = this._domIndex;
        if (!index) return null;

        const score = new Map<string, number>();
        scope.querySelectorAll('[id]').forEach((el) => {
            for (const helpId of index[(el as HTMLElement).id] ?? []) {
                score.set(helpId, (score.get(helpId) ?? 0) + 1);
            }
        });

        let best: string | null = null;
        let bestScore = 0;
        for (const [helpId, n] of score) {
            if (n > bestScore) {
                best = helpId;
                bestScore = n;
            }
        }
        return best;
    }

    /** Loaded once, on the first press that happens over a dialog. */
    private _ensureDomIndex(): Promise<void> {
        this._domIndexLoad ??= new Promise<void>((resolve) => {
            this._http.get<DomIndex>('assets/help/index.json').subscribe({
                next: (index) => {
                    this._domIndex = index?.dom ?? {};
                    this._hostIndex = index?.hosts ?? {};
                    resolve();
                },
                error: () => {
                    this._domIndex = {};
                    resolve();
                },
            });
        });
        return this._domIndexLoad;
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
