import { Component, OnInit, OnDestroy, AfterViewInit, ElementRef, inject, viewChild, signal } from '@angular/core';
import { HostDashboardServiceProxy, GetEditionTenantStatisticsOutput } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { filter as _filter } from 'lodash-es';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { WidgetOnResizeEventHandler, WIDGETONRESIZEEVENTHANDLERTOKEN } from '../../customizable-dashboard.component';
import { NgScrollbar } from 'ngx-scrollbar';

import { PieChartModule } from '@swimlane/ngx-charts';
import { LuxonFormatPipe } from '../../../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'app-widget-edition-statistics',
    templateUrl: './widget-edition-statistics.component.html',
    styleUrls: ['./widget-edition-statistics.component.css'],
    imports: [NgScrollbar, PieChartModule, LuxonFormatPipe, LocalizePipe],
})
/* eslint-disable brace-style */
export class WidgetEditionStatisticsComponent
    extends WidgetComponentBaseComponent
    implements OnInit, AfterViewInit, OnDestroy
{
    private _hostDashboardServiceProxy = inject(HostDashboardServiceProxy);
    private _dateTimeService = inject(DateTimeService);
    private _widgetOnResizeEventHandler = inject<WidgetOnResizeEventHandler>(WIDGETONRESIZEEVENTHANDLERTOKEN);
    private resizeObserver?: ResizeObserver;

    readonly editionStatisticsChart = viewChild<ElementRef>('EditionStatisticsChart');

    chartView: [number, number] = [0, 0];
    chartScale = 1;

    selectedDateRange: DateTime[] = [
        this._dateTimeService.getStartOfDayMinusDays(7).toDateTime(),
        this._dateTimeService.getEndOfDay().toDateTime(),
    ];

    readonly editionStatisticsHasData = signal(false);
    readonly editionStatisticsData = signal<any[]>([]);

    constructor() {
        super();
        const _widgetOnResizeEventHandler = this._widgetOnResizeEventHandler;

        _widgetOnResizeEventHandler.onResize.subscribe(() => {
            this.updateChartView();
            this.runDelayed(this.showChart);
        });
    }

    ngOnInit(): void {
        this.subDateRangeFilter();
        this.runDelayed(this.showChart);
    }

    ngAfterViewInit(): void {
        const host = this.editionStatisticsChart()?.nativeElement as HTMLElement | undefined;
        if (host && typeof ResizeObserver !== 'undefined') {
            this.resizeObserver = new ResizeObserver(() => this.updateChartView());
            this.resizeObserver.observe(host);
        }
        this.runDelayed(this.updateChartView);
    }

    ngOnDestroy(): void {
        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
            this.resizeObserver = undefined;
        }
    }

    showChart = () => {
        this._hostDashboardServiceProxy
            .getEditionTenantStatistics(this.selectedDateRange[0], this.selectedDateRange[1])
            .subscribe((editionTenantStatistics) => {
                const data = this.normalizeEditionStatisticsData(editionTenantStatistics);
                this.editionStatisticsData.set(data);
                this.editionStatisticsHasData.set(_filter(data, (d) => d.value > 0).length > 0);
                this.runDelayed(this.updateChartView);
            });
    };

    normalizeEditionStatisticsData(data: GetEditionTenantStatisticsOutput): Array<any> {
        if (!data || !data.editionStatistics || data.editionStatistics.length === 0) {
            return [];
        }

        const chartData = new Array(data.editionStatistics.length);

        for (let i = 0; i < data.editionStatistics.length; i++) {
            chartData[i] = {
                name: data.editionStatistics[i].label,
                value: data.editionStatistics[i].value,
            };
        }

        return chartData;
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
        this.runDelayed(this.showChart);
    };

    subDateRangeFilter() {
        this.subscribeToEvent('app.dashboardFilters.dateRangePicker.onDateChange', this.onDateRangeFilterChange);
    }

    private updateChartView = () => {
        const container = this.editionStatisticsChart()?.nativeElement as HTMLElement | undefined;
        if (!container) {
            return;
        }

        const width = Math.floor(container.clientWidth || 0);
        const heightFromContainer = Math.floor(container.clientHeight || 0);

        const minHeight = 220;
        const computedHeight = Math.max(minHeight, heightFromContainer || width);

        const scaledWidth = Math.floor(width * this.chartScale);
        const scaledHeight = Math.floor(computedHeight * this.chartScale);

        if (width > 0 && (this.chartView[0] !== scaledWidth || this.chartView[1] !== scaledHeight)) {
            this.chartView = [scaledWidth, scaledHeight];
        }
    };
}
