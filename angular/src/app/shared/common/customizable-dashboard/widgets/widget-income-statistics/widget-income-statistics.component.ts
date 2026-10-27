import { Component, OnInit, inject, signal } from '@angular/core';
import { ChartDateInterval, HostDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { filter as _filter } from 'lodash-es';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { WidgetOnResizeEventHandler, WIDGETONRESIZEEVENTHANDLERTOKEN } from '../../customizable-dashboard.component';
import { NgClass } from '@angular/common';
import { NgScrollbar } from 'ngx-scrollbar';
import { AreaChartModule } from '@swimlane/ngx-charts';
import { BusyIfDirective } from '../../../../../../shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '../../../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'app-widget-income-statistics',
    templateUrl: './widget-income-statistics.component.html',
    styleUrls: ['./widget-income-statistics.component.css'],
    imports: [NgClass, NgScrollbar, AreaChartModule, BusyIfDirective, LuxonFormatPipe, LocalizePipe],
})
export class WidgetIncomeStatisticsComponent extends WidgetComponentBaseComponent implements OnInit {
    private _hostDashboardServiceProxy = inject(HostDashboardServiceProxy);
    private _dateTimeService = inject(DateTimeService);
    private _widgetOnResizeEventHandler = inject<WidgetOnResizeEventHandler>(WIDGETONRESIZEEVENTHANDLERTOKEN);

    selectedIncomeStatisticsDateInterval = ChartDateInterval.Daily;
    readonly loadingIncomeStatistics = signal(true);

    selectedDateRange: DateTime[] = [
        this._dateTimeService.getStartOfDayMinusDays(7).toDateTime(),
        this._dateTimeService.getEndOfDay().toDateTime(),
    ];
    readonly incomeStatisticsData = signal<any[]>([]);
    readonly incomeStatisticsHasData = signal(false);
    appIncomeStatisticsDateInterval = ChartDateInterval;

    constructor() {
        super();
        const _widgetOnResizeEventHandler = this._widgetOnResizeEventHandler;

        _widgetOnResizeEventHandler.onResize.subscribe(() => {
            this.runDelayed(this.loadIncomeStatisticsData);
        });
    }

    ngOnInit() {
        this.subDateRangeFilter();
        this.runDelayed(this.loadIncomeStatisticsData);
    }

    incomeStatisticsDateIntervalChange(interval: number) {
        if (this.selectedIncomeStatisticsDateInterval === interval) {
            return;
        }

        this.selectedIncomeStatisticsDateInterval = interval;
        this.loadIncomeStatisticsData();
    }

    loadIncomeStatisticsData = () => {
        this.loadingIncomeStatistics.set(true);
        this._hostDashboardServiceProxy
            .getIncomeStatistics(
                this.selectedIncomeStatisticsDateInterval,
                this.selectedDateRange[0],
                this.selectedDateRange[1]
            )
            .subscribe((result) => {
                const data = this.normalizeIncomeStatisticsData(result.incomeStatistics);
                this.incomeStatisticsData.set(data);
                this.incomeStatisticsHasData.set(_filter(data[0].series, (d) => d.value > 0).length > 0);
                this.loadingIncomeStatistics.set(false);
            });
    };

    normalizeIncomeStatisticsData(data): any {
        const chartData = [];
        for (let i = 0; i < data.length; i++) {
            chartData.push({
                name: this._dateTimeService.formatISODateString(data[i].date, 'D'),
                value: data[i].amount,
            });
        }

        return [
            {
                name: '',
                series: chartData,
            },
        ];
    }

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
        this.runDelayed(this.loadIncomeStatisticsData);
    };

    subDateRangeFilter() {
        this.subscribeToEvent('app.dashboardFilters.dateRangePicker.onDateChange', this.onDateRangeFilterChange);
    }
}
