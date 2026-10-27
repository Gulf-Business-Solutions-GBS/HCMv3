import { Injectable, inject, signal } from '@angular/core';
import { CostElementSlotDto, CostElementSlotsServiceProxy } from '@shared/service-proxies/service-proxies';
import { Observable, of } from 'rxjs';
import { finalize, map, shareReplay, tap } from 'rxjs/operators';

/** The record fields the seven slots write to, in slot order (slot N -> index N-1). */
export const COST_ELEMENT_SLOT_FIELDS = [
    'costElement1Id',
    'costElement2Id',
    'costElement3Id',
    'costElement4Id',
    'costElement5Id',
    'costElement6Id',
    'costElement7Id',
] as const;

export type CostElementSlotField = (typeof COST_ELEMENT_SLOT_FIELDS)[number];

/**
 * The configured cost element slots (legacy CostController): slot N is captioned with the cost element type
 * whose Code is "CostElementN", unconfigured slots are not shown, and each slot lists only cost elements of
 * its type. Shared by the wage account group detail, cost register detail and employee wage account screens.
 *
 * Loaded once and kept for the session; a modal calls refresh() when it opens, so a type added a moment ago
 * in Cost Element Types shows up without reloading the app.
 */
@Injectable({ providedIn: 'root' })
export class CostElementSlotsService {
    private _proxy = inject(CostElementSlotsServiceProxy);

    readonly slots = signal<CostElementSlotDto[]>([]);
    readonly loaded = signal<boolean>(false);

    private _request: Observable<CostElementSlotDto[]> | undefined;

    /** Loads the slots unless they already are. */
    ensureLoaded(): Observable<CostElementSlotDto[]> {
        if (this.loaded()) {
            return of(this.slots());
        }
        return this.refresh();
    }

    /** Fetches the slots again; concurrent callers share one request. */
    refresh(): Observable<CostElementSlotDto[]> {
        if (!this._request) {
            this._request = this._proxy.getSlots().pipe(
                map((result) => result.items ?? []),
                tap((slots) => {
                    this.slots.set(slots);
                    this.loaded.set(true);
                }),
                finalize(() => (this._request = undefined)),
                shareReplay(1)
            );
        }
        return this._request;
    }

    fieldOf(slot: CostElementSlotDto): CostElementSlotField {
        return COST_ELEMENT_SLOT_FIELDS[slot.slot - 1];
    }

    /** The slot's caption in the reader's language. */
    label(slot: CostElementSlotDto): string {
        return this.pick(slot.nameEn, slot.nameAr) || slot.code;
    }

    /** Display name of the cost element stored in this slot of a row, for list columns. */
    valueName(slot: CostElementSlotDto, record: unknown): string {
        const id = (record as Record<string, number | undefined>)?.[this.fieldOf(slot)];
        if (!id) {
            return '';
        }
        const option = slot.options?.find((o) => o.id === id);
        // A value whose element changed type since it was saved is still shown, by id, rather than hidden.
        return option ? this.pick(option.nameEn, option.nameAr) || option.code : `#${id}`;
    }

    pick(en: string | undefined, ar: string | undefined): string {
        const arabic = abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
        return arabic ? ar || en || '' : en || '';
    }
}
