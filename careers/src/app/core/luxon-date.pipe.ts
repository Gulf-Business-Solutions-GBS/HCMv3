import { Pipe, PipeTransform } from '@angular/core';
import { DateTime } from 'luxon';

/**
 * Formats the Luxon `DateTime` values that NSwag generates.
 *
 * Angular's built-in `DatePipe` does NOT accept a Luxon `DateTime` (it is not a `Date` and not a number), so
 * `{{ vacancy.closingDate | date }}` is a compile error against these proxies. This pipe is the portal's
 * single date-rendering path.
 *
 * `format` accepts a Luxon preset name (`DATE_MED`, `DATE_FULL`, ...) and defaults to a medium date, which
 * is what every date on this portal is: a day, never a time.
 */
@Pipe({ name: 'careersDate' })
export class LuxonDatePipe implements PipeTransform {
    transform(value: DateTime | null | undefined, format: 'DATE_MED' | 'DATE_FULL' | 'DATE_SHORT' = 'DATE_MED'): string {
        if (!value || !value.isValid) {
            return '';
        }

        return value.toLocaleString(DateTime[format]);
    }
}
