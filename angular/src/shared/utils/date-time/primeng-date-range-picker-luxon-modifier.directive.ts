import { Directive, OnInit, inject, input, output, OnChanges, SimpleChanges, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePicker } from 'primeng/datepicker';
import { DateTime } from 'luxon';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import compare from 'just-compare';

@Directive({
    selector: 'p-datepicker[dateRangePickerLuxonModifier]',
})
export class PrimengDateRangePickerLuxonModifierDirective implements OnInit, OnChanges {
    private datePicker = inject(DatePicker);
    private _dateTimeService = inject(DateTimeService);
    private _destroyRef = inject(DestroyRef);
    private lastDates: Date[] | null = null;

    readonly dateRange = input<DateTime[] | Date[] | null>(null);
    readonly dateRangeChange = output<DateTime[]>();

    ngOnInit(): void {
        this.datePicker.onSelect.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((event: Date) => {
            const value = this.datePicker.value;
            if (Array.isArray(value) && value.length === 2 && value[0] && value[1]) {
                this.handleDateRangeChange(value as Date[]);
            }
        });

        this.datePicker.onClear.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => {
            this.lastDates = null;
            this.dateRangeChange.emit([]);
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['dateRange']) {
            const value = changes['dateRange'].currentValue;
            if (value && Array.isArray(value) && value.length === 2) {
                const dates: Date[] = [];

                if (value[0] instanceof DateTime) {
                    dates[0] = value[0].toJSDate();
                } else if (value[0] instanceof Date) {
                    dates[0] = value[0];
                }

                if (value[1] instanceof DateTime) {
                    dates[1] = value[1].toJSDate();
                } else if (value[1] instanceof Date) {
                    dates[1] = value[1];
                }

                if (dates[0] && dates[1] && !compare(this.lastDates, dates)) {
                    this.datePicker.writeValue(dates);
                }
            } else {
                this.datePicker.writeValue(null);
            }
        }
    }

    private handleDateRangeChange(dates: Date[]): void {
        if (!dates || dates.length !== 2 || !dates[0] || !dates[1]) {
            return;
        }

        if (
            dates[0] instanceof Date &&
            dates[1] instanceof Date &&
            dates[0].toString() !== 'Invalid Date' &&
            dates[1].toString() !== 'Invalid Date' &&
            !compare(this.lastDates, dates)
        ) {
            const processedDates = [
                this.clearTime(new Date(dates[0].getTime())),
                this.clearTime(new Date(dates[1].getTime())),
            ];

            this.lastDates = processedDates;

            if (abp.clock.provider.supportsMultipleTimezone) {
                this.lastDates = [
                    this._dateTimeService.changeTimeZone(processedDates[0], abp.timing.timeZoneInfo.iana.timeZoneId),
                    this._dateTimeService.changeTimeZone(processedDates[1], abp.timing.timeZoneInfo.iana.timeZoneId),
                ];
            }

            const startDate = this._dateTimeService.fromJSDate(this.lastDates[0]);
            const endDate = this._dateTimeService.fromJSDate(this.lastDates[1]);
            this.dateRangeChange.emit([startDate, endDate]);
        }
    }

    private clearTime(date: Date): Date {
        date.setHours(0);
        date.setMinutes(0);
        date.setSeconds(0);
        date.setMilliseconds(0);
        return date;
    }
}
