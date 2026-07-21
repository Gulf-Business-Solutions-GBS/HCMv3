import { Directive, OnInit, inject, input, output, OnChanges, SimpleChanges, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePicker } from 'primeng/datepicker';
import { DateTime } from 'luxon';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';

@Directive({
    selector: 'p-datepicker[datePickerLuxonModifier]',
})
export class PrimengDatePickerLuxonModifierDirective implements OnInit, OnChanges {
    private datePicker = inject(DatePicker);
    private _dateTimeService = inject(DateTimeService);
    private _destroyRef = inject(DestroyRef);
    private lastValue: Date | null = null;

    readonly date = input<DateTime | Date | null>(null);
    readonly isUtc = input(false);
    readonly withTimepicker = input(false);
    readonly dateChange = output<DateTime | null>();

    ngOnInit(): void {
        this.datePicker.onSelect.pipe(takeUntilDestroyed(this._destroyRef)).subscribe((event: Date) => {
            this.handleDateChange(event);
        });

        this.datePicker.onClear.pipe(takeUntilDestroyed(this._destroyRef)).subscribe(() => {
            this.lastValue = null;
            this.dateChange.emit(null);
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['date']) {
            const value = changes['date'].currentValue;
            if (value) {
                if (value instanceof DateTime) {
                    this.datePicker.writeValue(value.toJSDate());
                } else if (value instanceof Date) {
                    this.datePicker.writeValue(value);
                }
            } else {
                this.datePicker.writeValue(null);
            }
        }
    }

    private handleDateChange(date: Date | null): void {
        if (!date) {
            this.lastValue = null;
            this.dateChange.emit(null);
            return;
        }

        if (date instanceof Date && date.toString() !== 'Invalid Date') {
            const processedDate = new Date(date.getTime());

            if (!this.withTimepicker()) {
                processedDate.setHours(0);
                processedDate.setMinutes(0);
            }
            processedDate.setSeconds(0);
            processedDate.setMilliseconds(0);

            let luxonDate: DateTime;

            if (this.isUtc()) {
                luxonDate = DateTime.utc(
                    processedDate.getFullYear(),
                    processedDate.getMonth() + 1,
                    processedDate.getDate(),
                    processedDate.getHours(),
                    processedDate.getMinutes(),
                    0,
                    0
                );
            } else if (abp.clock.provider.supportsMultipleTimezone) {
                this.lastValue = this._dateTimeService.changeTimeZone(
                    processedDate,
                    abp.timing.timeZoneInfo.iana.timeZoneId
                );
                luxonDate = this._dateTimeService.fromJSDate(this.lastValue);
            } else {
                luxonDate = DateTime.fromJSDate(processedDate);
            }

            this.lastValue = luxonDate.toJSDate();
            this.dateChange.emit(luxonDate);
        }
    }
}
