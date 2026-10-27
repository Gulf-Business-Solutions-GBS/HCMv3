import { Signal, WritableSignal, effect, inject, untracked } from '@angular/core';
import { DateTime } from 'luxon';
import { EffectiveDatedPrefillService } from './effective-dated-prefill.service';

export interface EffectiveDatedPrefillOptions<T> {
    /** The record type, as the server names it: "EmployeeBasicPay", "TicketFare" and so on. */
    recordType: string;

    /** The form's record. Replaced with a filled-in copy when the effective date changes. */
    record: WritableSignal<T>;

    /** True while adding; an existing record is never refilled -- its values ARE the record. */
    isNew: Signal<boolean>;

    /** Whose history to read: the employee, and anything else that splits it, such as the wage type. */
    keys: () => { [name: string]: number | undefined | null };

    /** The effective date field on the record. Defaults to startDate, which is what every screen uses today. */
    dateField?: string;

    /** Called with the date the values came from, so a screen can say where they are from. */
    onPrefilled?: (sourceDate: DateTime) => void;
}

/**
 * Makes a form fill itself from the record in force on the effective date the user types.
 *
 * Call it once in a modal's field initialisers or constructor. It watches the date on the form's record and, while
 * adding, replaces the other fields with the values of the record that covered that date -- 01/01/2027 takes the
 * 01/01/2026 values, a back-dated 01/07/2025 takes the 01/01/2025 ones. The user then changes what they meant to.
 *
 * The date the user typed and the keys are kept as they are; only the remaining fields are replaced.
 */
export function attachEffectiveDatedPrefill<T extends object>(options: EffectiveDatedPrefillOptions<T>): void {
    const prefillService = inject(EffectiveDatedPrefillService);
    const dateField = options.dateField ?? 'startDate';

    // What was last looked up, so re-rendering or the fill itself cannot set off another round.
    let lastLookedUp: string | null = null;

    effect(() => {
        const record = options.record() as any;
        const date: DateTime | undefined = record?.[dateField];
        const isNew = options.isNew();

        if (!isNew || !date || !date.isValid) {
            lastLookedUp = null;
            return;
        }

        const keys = options.keys();
        const stamp = date.toISODate() + '|' + JSON.stringify(keys);
        if (stamp === lastLookedUp) {
            return;
        }

        lastLookedUp = stamp;

        untracked(() => {
            // A copy, so the form does not repaint field by field as the values land.
            const filled = Object.assign(Object.create(Object.getPrototypeOf(record)), record);

            prefillService.prefill(options.recordType, keys, date, filled).subscribe((sourceDate) => {
                if (!sourceDate) {
                    return;
                }

                filled[dateField] = date;
                options.record.set(filled);
                options.onPrefilled?.(sourceDate);
            });
        });
    });
}
