import { ChangeDetectionStrategy, Component, forwardRef, inject, input, signal } from '@angular/core';
import { FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { DateTime } from 'luxon';
import { DatePickerModule } from 'primeng/datepicker';
import { AbstractNgModelComponent } from '../../ng-model.component';
import { DateTimeService } from '../date-time.service';
import compare from 'just-compare';

@Component({
    selector: 'abp-datetime-picker',
    templateUrl: './abp-datetime-picker.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => AbpDateTimePickerComponent),
            multi: true,
        },
        { provide: DateTimeService, useClass: DateTimeService },
    ],
    imports: [DatePickerModule, FormsModule],
})
export class AbpDateTimePickerComponent extends AbstractNgModelComponent {
    private _dateTimeService = inject(DateTimeService);

    readonly isUtc = input(false);
    readonly withTimepicker = input(false);
    readonly placeholder = input<string>('');
    readonly showIcon = input(true);

    readonly date = signal<DateTime | null>(null);
    readonly lastDate = signal<Date | null>(null);

    setDate(date: Date | null): void {
        if (!date) {
            this.writeValue(null);
            return;
        }

        if (date instanceof Date && !compare(this.lastDate(), date) && date.toString() !== 'Invalid Date') {
            // clear hour, minute, seconds and milliseconds based on timepicker setting
            date = this.clearTime(date);

            if (this.isUtc()) {
                // using UTC date regardless of timing configuration
                const utcDateString = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString();
                date = this._dateTimeService.fromISODateString(utcDateString).toJSDate();
            } else if (abp.clock.provider.supportsMultipleTimezone) {
                date = this._dateTimeService.changeTimeZone(date, abp.timing.timeZoneInfo.iana.timeZoneId);
            }

            this.writeValue(this._dateTimeService.fromJSDate(date));
        }
    }

    writeValue(value: DateTime | null): void {
        this.date.set(value);
        if (!value) {
            this.cleanValue();
            return;
        }

        this.lastDate.set(this._dateTimeService.toJSDate(value));
        super.writeValue(value);
        this.change();
    }

    change(): void {
        const currentDate = this.date();
        if (!currentDate) {
            return;
        }

        this._value = currentDate;
        this.notifyValueChange();
    }

    cleanValue(): void {
        this.date.set(null);
        this._value = null;
        this.lastDate.set(null);
    }

    clearTime(date: Date): Date {
        const newDate = new Date(date.getTime());
        if (!this.withTimepicker()) {
            newDate.setHours(0);
            newDate.setMinutes(0);
        }

        newDate.setSeconds(0);
        newDate.setMilliseconds(0);
        return newDate;
    }
}
