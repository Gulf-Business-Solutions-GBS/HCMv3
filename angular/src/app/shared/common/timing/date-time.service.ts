import { Injectable, inject } from '@angular/core';
import { AppLocalizationService } from '@app/shared/common/localization/app-localization.service';
import { DateTime } from 'luxon';
import { ChainableDateTime } from './ChainableDateTime';

@Injectable({ providedIn: 'root' })
export class DateTimeService {
    private _appLocalizationService = inject(AppLocalizationService);

    createDateRangePickerOptions(): any {
        const options = {
            locale: {
                format: 'L',
                applyLabel: this._appLocalizationService.l('Apply'),
                cancelLabel: this._appLocalizationService.l('Cancel'),
                customRangeLabel: this._appLocalizationService.l('CustomRange'),
            },
            min: this.fromISODateString('1900-01-01'),
            minDate: this.fromISODateString('1900-01-01'),
            max: this.getDate(),
            maxDate: this.getDate(),
            opens: 'left',
            ranges: {},
        };

        options.ranges[this._appLocalizationService.l('Today')] = [this.getStartOfDay(), this.getEndOfDay()];
        options.ranges[this._appLocalizationService.l('Yesterday')] = [
            this.getStartOfDay().minusDays(1),
            this.getEndOfDay().minusDays(1),
        ];
        options.ranges[this._appLocalizationService.l('Last7Days')] = [
            this.getStartOfDay().minusDays(6),
            this.getEndOfDay(),
        ];
        options.ranges[this._appLocalizationService.l('Last30Days')] = [
            this.getStartOfDay().minusDays(29),
            this.getEndOfDay(),
        ];
        options.ranges[this._appLocalizationService.l('ThisMonth')] = [
            this.getDate().startOf('month'),
            this.getDate().endOf('month'),
        ];
        options.ranges[this._appLocalizationService.l('LastMonth')] = [
            this.getDate().minus({ months: 1 }).startOf('month'),
            this.getDate().minus({ months: 1 }).endOf('month'),
        ];

        return options;
    }

    /** Core chainable getters */
    getDate(): ChainableDateTime {
        const dt = abp.clock.provider.supportsMultipleTimezone
            ? DateTime.local().setZone(abp.timing.timeZoneInfo.iana.timeZoneId)
            : DateTime.local();
        return new ChainableDateTime(dt);
    }

    getUTCDate(): ChainableDateTime {
        return new ChainableDateTime(DateTime.utc());
    }

    getYear(): number {
        return this.getDate().value.year;
    }

    getStartOfDay(): ChainableDateTime {
        return this.getDate().startOf('day');
    }

    getStartOfWeek(): ChainableDateTime {
        return this.getDate().startOf('week');
    }

    getStartOfDayForDate(date: DateTime | Date): ChainableDateTime {
        if (!date) {
            return ChainableDateTime.from(DateTime.invalid('Invalid Date'));
        }
        return ChainableDateTime.from(date).startOf('day');
    }

    getStartOfDayMinusDays(daysFromNow: number): ChainableDateTime {
        return this.getDate().minusDays(daysFromNow).startOf('day');
    }

    getEndOfDay(): ChainableDateTime {
        return this.getDate().endOf('day');
    }

    getEndOfDayForDate(date: DateTime | Date): ChainableDateTime {
        if (!date) {
            return ChainableDateTime.from(DateTime.invalid('Invalid Date'));
        }
        return ChainableDateTime.from(date).endOf('day');
    }

    getEndOfDayPlusDays(daysFromNow: number): ChainableDateTime {
        return this.getDate().plusDays(daysFromNow).endOf('day');
    }

    getEndOfDayMinusDays(daysFromNow: number): ChainableDateTime {
        return this.getDate().minusDays(daysFromNow).endOf('day');
    }

    /** Chainable math helpers */
    plusDays(date: DateTime | Date, dayCount: number): ChainableDateTime {
        return ChainableDateTime.from(date).plusDays(dayCount);
    }

    plusSeconds(date: DateTime | Date, seconds: number): ChainableDateTime {
        if (!date) {
            return ChainableDateTime.from(DateTime.invalid('Invalid Date'));
        }
        return ChainableDateTime.from(date).plusSeconds(seconds);
    }

    minusDays(date: DateTime | Date, dayCount: number): ChainableDateTime {
        return ChainableDateTime.from(date).minusDays(dayCount);
    }

    /** Read-only helpers (not chainable) */
    fromISODateString(date: string): DateTime {
        return DateTime.fromISO(date);
    }

    formatISODateString(dateText: string, format: string): string {
        return DateTime.fromISO(dateText).toFormat(format);
    }

    formatJSDate(jsDate: Date, format: string): string {
        return DateTime.fromJSDate(jsDate).toFormat(format);
    }

    formatDate(date: DateTime | Date, format: string): string {
        if (date instanceof Date) {
            return this.formatDate(this.fromJSDate(date), format);
        }
        return date.toFormat(format);
    }

    getDiffInSeconds(maxDate: DateTime | Date, minDate: DateTime | Date) {
        if (maxDate instanceof Date && minDate instanceof Date) {
            return this.getDiffInSeconds(this.fromJSDate(maxDate), this.fromJSDate(minDate));
        }
        return (maxDate as DateTime).diff(minDate as DateTime, 'seconds').seconds;
    }

    createJSDate(year: number, month: number, day: number): Date {
        return this.createDate(year, month, day).toJSDate();
    }

    createDate(year: number, month: number, day: number): DateTime {
        return abp.clock.provider.supportsMultipleTimezone
            ? DateTime.utc(year, month + 1, day)
            : DateTime.local(year, month + 1, day);
    }

    createUtcDate(year: number, month: number, day: number): DateTime {
        return DateTime.utc(year, month + 1, day);
    }

    toUtcDate(date: DateTime | Date): DateTime {
        if (date instanceof Date) {
            return this.createUtcDate(date.getFullYear(), date.getMonth(), date.getDate());
        }
        return (date as DateTime).toUTC();
    }

    toJSDate(date: DateTime): Date {
        return date.toJSDate();
    }

    fromJSDate(date: Date): DateTime {
        return DateTime.fromJSDate(date);
    }

    fromNow(date: DateTime | Date): string | null {
        if (date instanceof Date) {
            return this.fromNow(this.fromJSDate(date));
        }
        return (date as DateTime).toRelative();
    }

    getTimezoneOffset(ianaTimezoneId: string): number {
        return DateTime.local({ zone: ianaTimezoneId }).offset;
    }

    changeTimeZone(date: Date, ianaTimezoneId: string): Date {
        return DateTime.fromJSDate(date).setZone(ianaTimezoneId).toJSDate();
    }

    changeDateTimeZone(date: DateTime, ianaTimezoneId: string): DateTime {
        return date.setZone(ianaTimezoneId);
    }
}
