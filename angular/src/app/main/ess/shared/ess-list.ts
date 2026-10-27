import { Signal, computed, signal } from '@angular/core';

/**
 * One column of a self-service request list, as the shared heading, filter row and pager need to know it.
 *
 * <p><c>text</c> is what the cell SHOWS, because that is what a person filters by: typing "11/2026" should
 * find November whatever the date is underneath. <c>sort</c> is only needed when the shown text would sort
 * wrongly -- dates and numbers.</p>
 */
export interface EssListColumn<T> {
    /** Unique within the list; any short name. */
    key: string;

    /** Localization key of the heading. Empty for an untitled column, which then has no sorting. */
    label: string;

    text: (row: T) => string | number | undefined | null;

    sort?: (row: T) => string | number | undefined | null;

    /** A box to type in (the default), a dropdown of the values present, or no filter at all. */
    filter?: 'text' | 'select' | 'none';

    /** 'end' for a column of amounts, so the heading sits over the figures. */
    align?: 'end';
}

/**
 * Filtering, sorting and paging for a self-service request list, the same on every screen.
 *
 * <p>Works on the rows the screen has already loaded rather than asking the server again: an employee's own
 * requests of one kind are a short list, and every one of these screens loads it whole.</p>
 */
export class EssList<T> {
    readonly pageSizes = [10, 25, 50];

    readonly filters = signal<Record<string, string>>({});
    readonly sortKey = signal<string | undefined>(undefined);
    readonly sortDescending = signal(false);
    readonly page = signal(0);
    readonly pageSize = signal(10);

    /** Read lazily: the labels and accessors usually call into the component, which is not built yet. */
    private readonly _columns = computed(() => this.columnsFactory());

    /** Everything the filter row lets through, in the chosen order. */
    readonly filtered = computed(() => {
        const columns = this._columns();
        const filters = this.filters();

        const rows = this.rows().filter((row) =>
            columns.every((column) => {
                const wanted = (filters[column.key] ?? '').trim().toLowerCase();
                if (!wanted) {
                    return true;
                }

                const shown = `${column.text(row) ?? ''}`.toLowerCase();

                return column.filter === 'select' ? shown === wanted : shown.includes(wanted);
            })
        );

        const sortColumn = columns.find((column) => column.key === this.sortKey());
        if (!sortColumn) {
            return rows;
        }

        const valueOf = sortColumn.sort ?? sortColumn.text;
        const direction = this.sortDescending() ? -1 : 1;

        return [...rows].sort((a, b) => direction * EssList.compare(valueOf(a), valueOf(b)));
    });

    readonly total = computed(() => this.filtered().length);

    /** The page on screen. */
    readonly view = computed(() => {
        const size = this.pageSize();

        // A filter or a reload can leave the page past the end; show the last page that exists instead.
        const lastPage = Math.max(0, Math.ceil(this.total() / size) - 1);
        const page = Math.min(this.page(), lastPage);

        return this.filtered().slice(page * size, page * size + size);
    });

    /**
     * Set by a screen whose export is not simply "the rows as shown" -- leave, whose rows have requests
     * underneath. Left unset, Export to Excel sends the list as it stands on screen.
     */
    exporter?: () => void;

    constructor(
        private readonly rows: Signal<T[]>,
        private readonly columnsFactory: () => EssListColumn<T>[]
    ) {}

    get columns(): EssListColumn<T>[] {
        return this._columns();
    }

    /** The values a dropdown filter offers: the ones actually present in the list. */
    optionsOf(column: EssListColumn<T>): string[] {
        return [...new Set(this.rows().map((row) => `${column.text(row) ?? ''}`))].filter((value) => !!value).sort();
    }

    setFilter(key: string, value: string): void {
        this.filters.update((current) => ({ ...current, [key]: value ?? '' }));
        this.page.set(0);
    }

    clearFilters(): void {
        this.filters.set({});
        this.page.set(0);
    }

    /** First click sorts ascending, the second descending, the third puts the list back as it came. */
    toggleSort(key: string): void {
        if (this.sortKey() !== key) {
            this.sortKey.set(key);
            this.sortDescending.set(false);
        } else if (!this.sortDescending()) {
            this.sortDescending.set(true);
        } else {
            this.sortKey.set(undefined);
            this.sortDescending.set(false);
        }

        this.page.set(0);
    }

    private static compare(a: string | number | undefined | null, b: string | number | undefined | null): number {
        if (a === undefined || a === null || a === '') {
            return b === undefined || b === null || b === '' ? 0 : -1;
        }

        if (b === undefined || b === null || b === '') {
            return 1;
        }

        if (typeof a === 'number' && typeof b === 'number') {
            return a - b;
        }

        return `${a}`.localeCompare(`${b}`, undefined, { numeric: true, sensitivity: 'base' });
    }
}
