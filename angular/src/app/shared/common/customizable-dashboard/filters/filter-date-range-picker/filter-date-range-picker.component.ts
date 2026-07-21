import { Component, inject } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DateTime } from 'luxon';
import { DatePickerModule } from 'primeng/datepicker';
import { PrimengDateRangePickerLuxonModifierDirective } from '../../../../../../shared/utils/date-time/primeng-date-range-picker-luxon-modifier.directive';

@Component({
    selector: 'app-filter-date-range-picker',
    templateUrl: './filter-date-range-picker.component.html',
    styleUrls: ['./filter-date-range-picker.component.css'],
    imports: [DatePickerModule, PrimengDateRangePickerLuxonModifierDirective],
})
export class FilterDateRangePickerComponent extends AppComponentBase {
    private _dateTimeService = inject(DateTimeService);

    date: Date;
    selectedDateRange: DateTime[] = [
        this._dateTimeService.getStartOfDayMinusDays(7).toDateTime(),
        this._dateTimeService.getEndOfDay().toDateTime(),
    ];

    onChange() {
        abp.event.trigger('app.dashboardFilters.dateRangePicker.onDateChange', this.selectedDateRange);
    }
}
