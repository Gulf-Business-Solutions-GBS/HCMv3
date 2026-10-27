import { Injectable, inject } from '@angular/core';
import { DateTime } from 'luxon';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import {
    EffectiveDatedKeyDto,
    EffectiveDatedPrefillInput,
    EffectiveDatedPrefillServiceProxy,
} from '@shared/service-proxies/service-proxies';

/**
 * Fills a NEW date-effective record from the one in force on the date the user typed.
 *
 * Adding a salary effective 01/01/2027 starts from the 01/01/2026 values; adding one back-dated to 01/07/2025 starts
 * from the 01/01/2025 values. It is the record covering that date, not the latest one -- which is what legacy got
 * wrong, always reading the record in force today.
 *
 * Only on a new record: on an existing one the values ARE the record, and refilling would quietly rewrite it.
 */
@Injectable({ providedIn: 'root' })
export class EffectiveDatedPrefillService {
    private readonly _proxy = inject(EffectiveDatedPrefillServiceProxy);

    /**
     * Reads the values in force on `effectiveDate` and writes them into `record`, returning the date they came from
     * (null when the history starts later, in which case `record` is left as it is).
     *
     * `keys` names the history: the employee, and anything else that splits it, such as the wage type.
     */
    prefill(
        recordType: string,
        keys: { [name: string]: number | undefined | null },
        effectiveDate: DateTime | undefined | null,
        record: any
    ): Observable<DateTime | null> {
        const keyDtos = Object.keys(keys)
            .filter((name) => keys[name] !== undefined && keys[name] !== null)
            .map((name) => new EffectiveDatedKeyDto({ name, value: keys[name] }));

        // Without a date there is nothing to look up, and without every key the server would have to guess whose
        // history to read -- it refuses, so do not ask.
        if (!effectiveDate || !effectiveDate.isValid || keyDtos.length !== Object.keys(keys).length) {
            return of(null);
        }

        const input = new EffectiveDatedPrefillInput({
            recordType,
            keys: keyDtos,
            effectiveDate,
        });

        return this._proxy.getPrefill(input).pipe(
            map((result) => {
                if (!result.found) {
                    return null;
                }

                this.applyTo(record, result.values);
                return result.sourceDate ?? null;
            })
        );
    }

    /**
     * Copies the values onto the record, field by field, for fields the record actually has. Dates arrive as text and
     * are turned back into dates, because that is what the date inputs bind to.
     */
    private applyTo(record: any, values: { [key: string]: any } | undefined): void {
        if (!record || !values) {
            return;
        }

        for (const key of Object.keys(values)) {
            if (!(key in record)) {
                continue;
            }

            const value = values[key];
            record[key] = this.isIsoDate(value) ? DateTime.fromISO(value) : value;
        }
    }

    private isIsoDate(value: any): boolean {
        return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}(T|$)/.test(value);
    }
}
