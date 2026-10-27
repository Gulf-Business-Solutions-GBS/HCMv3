import { computed, inject, Signal, signal } from '@angular/core';
import { GridReportOrgLevelsServiceProxy } from '@shared/service-proxies/service-proxies';

/** A report column the helper can recaption: the shape every analysis grid report column already has. */
export interface OrgLevelCaptionColumn {
    field: string;
    header: string;
}

/** Report row fields `orgLevel1`..`orgLevel7`, plus the Arabic-name twins `orgLevel1Ar`..`orgLevel7Ar`. */
const ORG_LEVEL_FIELD = /^orgLevel([1-7])(Ar)?$/;

/**
 * Captions for the OrgLevel1..OrgLevel7 columns of the analysis grid reports.
 *
 * Legacy captioned each of those columns with the tenant's Organization Unit Type name for that level
 * (`OrgService.GetOrgUnitTypeColName`): "Company", "Division", "Department"... rather than "Org Level 1".
 * This loads those names once per report screen (`GridReportOrgLevels.GetOrgLevelCaptions`, which returns
 * only the levels the tenant has defined) and exposes them as signals, so the screen re-renders when
 * they arrive.
 *
 * - `caption(n)` -- header text for level n; the `OrgLevelN` localization text until loaded or on error.
 *   `arabicCaption(n)` is the same for the Arabic-name twin column ("Company (Arabic)").
 * - `isShown(n)` -- false for a level the tenant has not defined. Hidden too while the captions are still
 *   loading, so seven placeholder columns do not flash up and then vanish. If the call fails, every level
 *   is shown with the old `OrgLevelN` captions, i.e. exactly the behaviour before this helper.
 * - `columns(list)` -- for screens that declare their columns as data: a signal of the same list with the
 *   `orgLevelN` columns recaptioned and the undefined ones removed.
 *
 * Create it in a field initializer (it uses `inject`):
 *     readonly orgLevels = injectOrgLevelCaptions((key, ...args) => this.l(key, ...args));
 */
export class OrgLevelCaptions {
    /** null = not loaded yet; undefined = load failed; otherwise level -> caption. */
    private readonly _captions = signal<ReadonlyMap<number, string> | null | undefined>(null);

    constructor(private readonly _localize: (key: string, ...args: any[]) => string) {}

    /** True once the call has returned, successfully or not. */
    readonly loaded = computed(() => this._captions() !== null);

    setCaptions(captions: ReadonlyMap<number, string> | undefined): void {
        this._captions.set(captions);
    }

    caption(level: number): string {
        const captions = this._captions();
        return captions?.get(level) ?? this._localize('OrgLevel' + level);
    }

    /** Header of the `orgLevelNAr` column: legacy appended "Ar" to the type name; here "Company (Arabic)". */
    arabicCaption(level: number): string {
        const caption = this._captions()?.get(level);
        return caption === undefined
            ? this._localize('OrgLevel' + level + 'Ar')
            : this._localize('OrgLevelArabicCaption', caption);
    }

    isShown(level: number): boolean {
        const captions = this._captions();
        if (captions === null) {
            return false;
        }

        return captions === undefined || captions.has(level);
    }

    /**
     * The org level (1..7) a column field holds and whether it is the Arabic-name twin, or undefined when
     * it is not an `orgLevelN`/`orgLevelNAr` column.
     */
    static levelOf(field: string): { level: number; arabic: boolean } | undefined {
        const match = ORG_LEVEL_FIELD.exec(field);
        return match ? { level: Number(match[1]), arabic: match[2] === 'Ar' } : undefined;
    }

    /**
     * The given columns with every `orgLevelN`/`orgLevelNAr` column recaptioned and the tenant's undefined levels
     * removed. A column that carries a `localize: true` flag (its header is a key, translated at render
     * time) gets `localize: false`, because the caption is already display text.
     */
    apply<T extends OrgLevelCaptionColumn>(columns: readonly T[]): T[] {
        const result: T[] = [];

        for (const column of columns) {
            const orgLevel = OrgLevelCaptions.levelOf(column.field);
            if (orgLevel === undefined) {
                result.push(column);
                continue;
            }

            if (!this.isShown(orgLevel.level)) {
                continue;
            }

            const header = orgLevel.arabic ? this.arabicCaption(orgLevel.level) : this.caption(orgLevel.level);
            const recaptioned: T = { ...column, header };
            if ('localize' in recaptioned) {
                (recaptioned as T & { localize: boolean }).localize = false;
            }

            result.push(recaptioned);
        }

        return result;
    }

    /** A signal of {@link apply} over a fixed column list (or a function reading other signals). */
    columns<T extends OrgLevelCaptionColumn>(source: readonly T[] | (() => readonly T[])): Signal<T[]> {
        return computed(() => this.apply(typeof source === 'function' ? source() : source));
    }
}

/**
 * Creates an {@link OrgLevelCaptions} and starts loading the tenant's captions. Call from a field
 * initializer or constructor (injection context).
 */
export function injectOrgLevelCaptions(localize: (key: string, ...args: any[]) => string): OrgLevelCaptions {
    const proxy = inject(GridReportOrgLevelsServiceProxy);
    const orgLevels = new OrgLevelCaptions(localize);

    proxy.getOrgLevelCaptions().subscribe({
        next: (result) => {
            const captions = new Map<number, string>();
            for (const item of result.items ?? []) {
                captions.set(item.level, item.caption);
            }

            orgLevels.setCaptions(captions);
        },
        // Fall back to the old fixed captions with every level shown, never to a grid with no org columns.
        error: () => orgLevels.setCaptions(undefined),
    });

    return orgLevels;
}
