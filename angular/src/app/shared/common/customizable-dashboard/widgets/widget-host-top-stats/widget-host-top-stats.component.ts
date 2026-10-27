import { Component, OnInit, inject, signal } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { HostDashboardServiceProxy, TopStatsData } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { NgScrollbar } from 'ngx-scrollbar';
import { DecimalPipe, CurrencyPipe } from '@angular/common';
import { Angular2CountoModule } from '@awaismirza/angular2-counto';
import { LuxonFormatPipe } from '../../../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'app-widget-host-top-stats',
    templateUrl: './widget-host-top-stats.component.html',
    styleUrls: ['./widget-host-top-stats.component.css'],
    imports: [NgScrollbar, Angular2CountoModule, DecimalPipe, CurrencyPipe, LuxonFormatPipe, LocalizePipe],
})
export class WidgetHostTopStatsComponent extends WidgetComponentBaseComponent implements OnInit {
    private _hostDashboardServiceProxy = inject(HostDashboardServiceProxy);
    private _dateTimeService = inject(DateTimeService);

    readonly countoNewSubscriptionAmount = signal(0);
    readonly countoNewTenantsCount = signal(0);
    readonly countoDashboardPlaceholder1 = signal(0);
    readonly countoDashboardPlaceholder2 = signal(0);

    selectedDateRange: DateTime[] = [
        this._dateTimeService.getStartOfDayMinusDays(7).toDateTime(),
        this._dateTimeService.getEndOfDay().toDateTime(),
    ];
    readonly loading = signal(true);
    readonly topStatsData = signal<TopStatsData | undefined>(undefined);

    ngOnInit(): void {
        this.subDateRangeFilter();
        this.runDelayed(this.loadHostTopStatsData);
    }

    loadHostTopStatsData = () => {
        this._hostDashboardServiceProxy
            .getTopStatsData(this.selectedDateRange[0], this.selectedDateRange[1])
            .subscribe((data) => {
                this.topStatsData.set(data);
                this.loading.set(false);
            });
    };

    onDateRangeFilterChange = (dateRange) => {
        if (
            !dateRange ||
            dateRange.length !== 2 ||
            (this.selectedDateRange[0] === dateRange[0] && this.selectedDateRange[1] === dateRange[1])
        ) {
            return;
        }

        this.selectedDateRange[0] = dateRange[0];
        this.selectedDateRange[1] = dateRange[1];
        this.runDelayed(this.loadHostTopStatsData);
    };

    subDateRangeFilter() {
        this.subscribeToEvent('app.dashboardFilters.dateRangePicker.onDateChange', this.onDateRangeFilterChange);
    }
}
