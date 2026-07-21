import { Component, ElementRef, OnInit, inject, viewChild, signal } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DemoUiComponentsServiceProxy, SendAndGetDateWithTextInput } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { FormsModule } from '@angular/forms';
import { AbpDateTimePickerComponent } from '../../shared/common/timing/abp-datetime-picker/abp-datetime-picker.component';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { AbpDateRangePickerComponent } from '../../shared/common/timing/abp-date-range-picker/abp-date-range-picker.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'demo-ui-date-time',
    templateUrl: './demo-ui-date-time.component.html',
    animations: [appModuleAnimation()],
    imports: [
        FormsModule,
        AbpDateTimePickerComponent,
        ValidationMessagesComponent,
        AbpDateRangePickerComponent,
        LocalizePipe,
    ],
})
export class DemoUiDateTimeComponent extends AppComponentBase implements OnInit {
    private demoUiComponentsService = inject(DemoUiComponentsServiceProxy);
    private _dateTimeService = inject(DateTimeService);

    readonly sampleDatePicker = viewChild<ElementRef>('SampleDatePicker');
    readonly sampleDateTimePicker = viewChild<ElementRef>('SampleDateTimePicker');

    sampleDate = signal<DateTime>(null);
    sampleDateTime = signal<DateTime>(null);
    sampleDateRange = signal<DateTime[]>([]);
    sampleDateWithTextInput = signal<SendAndGetDateWithTextInput>(new SendAndGetDateWithTextInput());
    dateFormat = signal('yyyy-LL-dd HH:mm:ss');

    ngOnInit(): void {
        this.sampleDate.set(this._dateTimeService.getStartOfDay().toDateTime());
        this.sampleDateTime.set(this._dateTimeService.getStartOfDay().toDateTime());
        const input = new SendAndGetDateWithTextInput();
        input.text = 'This is a sample text';
        input.date = this._dateTimeService.getStartOfDay().toDateTime();
        this.sampleDateWithTextInput.set(input);
        this.sampleDateRange.set([
            this._dateTimeService.getStartOfDayMinusDays(7).toDateTime(),
            this._dateTimeService.getStartOfDay().toDateTime(),
        ]);
    }

    updateDateWithTextInputDate(date: DateTime): void {
        const input = this.sampleDateWithTextInput();
        input.date = date;
        this.sampleDateWithTextInput.set(input);
    }

    updateDateWithTextInputText(text: string): void {
        const input = this.sampleDateWithTextInput();
        input.text = text;
        this.sampleDateWithTextInput.set(input);
    }

    // default date picker - post
    submitDate(): void {
        this.demoUiComponentsService.sendAndGetDate(this.sampleDate()).subscribe((data) => {
            let dateString = this.getDateString(data.date);
            this.message.info(dateString, this.l('PostedValue'));
        });
    }

    // default date time picker - post
    submitDateTime(): void {
        this.demoUiComponentsService.sendAndGetDate(this.sampleDateTime()).subscribe((data) => {
            let dateString = this.getDateString(data.date);
            this.message.info(dateString, this.l('PostedValue'));
        });
    }

    // default date range picker - post
    submitDateRange(): void {
        const range = this.sampleDateRange();
        this.demoUiComponentsService.sendAndGetDateRange(range[0], range[1]).subscribe((data) => {
            let startDateString = this.getDateString(data.startDate);
            let endDateString = this.getDateString(data.endDate);
            this.message.info(startDateString + '-' + endDateString, this.l('PostedValue'));
        });
    }

    // default date picker with text - post
    submitDateWithText(): void {
        this.demoUiComponentsService.sendAndGetDateWithText(this.sampleDateWithTextInput()).subscribe((data) => {
            let dateString = this.getDateString(data.date);
            this.message.info(dateString + '-' + data.text, this.l('PostedValue'));
        });
    }

    getDateString(date: DateTime): string {
        let dateString = this._dateTimeService.formatDate(date, this.dateFormat());
        if (abp.clock.provider.supportsMultipleTimezone) {
            dateString += '(' + abp.timing.timeZoneInfo.iana.timeZoneId + ')';
        }

        return dateString;
    }
}
