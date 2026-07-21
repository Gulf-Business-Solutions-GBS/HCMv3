import { ChangeDetectionStrategy, Component, forwardRef, inject, input, viewChild } from '@angular/core';
import { NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { DatePicker, DatePickerModule } from 'primeng/datepicker';
import { AbstractNgModelComponent } from '../../ng-model.component';
import { DateTimeService } from '../date-time.service';
import compare from 'just-compare';

@Component({
    selector: 'abp-date-range-picker',
    templateUrl: './abp-date-range-picker.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => AbpDateRangePickerComponent),
            multi: true,
        },
        { provide: DateTimeService, useClass: DateTimeService },
    ],
    imports: [DatePickerModule, FormsModule],
})
export class AbpDateRangePickerComponent extends AbstractNgModelComponent {
    private _dateTimeService = inject(DateTimeService);

    readonly datePicker = viewChild<DatePicker>('datePicker');
    readonly placeholder = input<string>('');
    readonly baseZIndex = input<number>(10000);

    startDate: DateTime | null | undefined;
    endDate: DateTime | null | undefined;
    lastDates: Date[] = null;

    onSelect(event: Date): void {
        const value = this.datePicker()?.value;
        if (Array.isArray(value) && value.length === 2 && value[0] && value[1]) {
            this.setDate(value as Date[]);
        }
    }

    onClear(): void {
        this.writeValue(null);
    }

    setDate(dates: Date[]) {
        if (!dates) {
            this.writeValue(null);
        } else if (
            dates &&
            dates[0] instanceof Date &&
            dates[1] instanceof Date &&
            !compare(this.lastDates, dates) &&
            dates[0].toString() !== 'Invalid Date' &&
            dates[1].toString() !== 'Invalid Date'
        ) {
            // clear time info of given dates because DateRangePicker doesn't support selecting time
            dates[0] = this.clearTime(new Date(dates[0].getTime()));
            dates[1] = this.clearTime(new Date(dates[1].getTime()));

            this.lastDates = dates;

            if (abp.clock.provider.supportsMultipleTimezone) {
                this.lastDates = [
                    this._dateTimeService.changeTimeZone(dates[0], abp.timing.timeZoneInfo.iana.timeZoneId),
                    this._dateTimeService.changeTimeZone(dates[1], abp.timing.timeZoneInfo.iana.timeZoneId),
                ];
            }

            let startDate = this._dateTimeService.fromJSDate(this.lastDates[0]);
            let endDate = this._dateTimeService.fromJSDate(this.lastDates[1]);

            this.writeValue([startDate, endDate]);
        }
    }

    writeValue(values: DateTime[]) {
        if (!values) {
            this.cleanValue();
            return;
        }

        this.startDate = values[0];
        this.endDate = values[1];

        this.lastDates = [this._dateTimeService.toJSDate(this.startDate), this._dateTimeService.toJSDate(this.endDate)];

        super.writeValue(values);
        this.change();
    }

    change() {
        if (!this.startDate && !this.endDate) {
            return;
        }

        this._value = [this.startDate, this.endDate];
    }

    cleanValue() {
        this.startDate = null;
        this.endDate = null;
        this._value = null;
        this.lastDates = null;
    }

    clearTime(date: Date): Date {
        date.setHours(0);
        date.setMinutes(0);
        date.setSeconds(0);
        date.setMilliseconds(0);
        return date;
    }
}
