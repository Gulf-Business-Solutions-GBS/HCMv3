import { Component, OnInit, inject, signal } from '@angular/core';
import { TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetOnResizeEventHandler, WIDGETONRESIZEEVENTHANDLERTOKEN } from '../../customizable-dashboard.component';
import { DashboardChartBase } from '../dashboard-chart-base';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { PieChartModule } from '@swimlane/ngx-charts';
import { NgStyle } from '@angular/common';

class ProfitSharePieChart extends DashboardChartBase {
    readonly chartData = signal<any[]>([]);
    scheme: any = {
        name: 'custom',
        selectable: true,
        group: 'Ordinal',
        domain: ['#00c5dc', '#ffb822', '#716aca'],
    };

    constructor(private _dashboardService: TenantDashboardServiceProxy) {
        super();
    }

    init(data: number[]) {
        let formattedData = [];
        for (let i = 0; i < data.length; i++) {
            formattedData.push({
                name: this.getChartItemName(i),
                value: data[i],
                color: this.scheme.domain[i],
            });
        }

        this.chartData.set(formattedData);
    }

    getChartItemName(index: number) {
        if (index === 0) {
            return 'Product Sales';
        }

        if (index === 1) {
            return 'Online Courses';
        }

        if (index === 2) {
            return 'Custom Development';
        }

        return 'Other';
    }

    reload() {
        this.showLoading();
        this._dashboardService.getProfitShare().subscribe((data) => {
            this.init(data.profitShares);
            this.hideLoading();
        });
    }
}

@Component({
    selector: 'app-widget-profit-share',
    templateUrl: './widget-profit-share.component.html',
    styleUrls: ['./widget-profit-share.component.css'],
    imports: [PieChartModule, NgStyle],
})
export class WidgetProfitShareComponent extends WidgetComponentBaseComponent implements OnInit {
    private _dashboardService = inject(TenantDashboardServiceProxy);
    private _widgetOnResizeEventHandler = inject<WidgetOnResizeEventHandler>(WIDGETONRESIZEEVENTHANDLERTOKEN);

    profitSharePieChart: ProfitSharePieChart;

    constructor() {
        super();
        const _widgetOnResizeEventHandler = this._widgetOnResizeEventHandler;

        this.profitSharePieChart = new ProfitSharePieChart(this._dashboardService);

        _widgetOnResizeEventHandler.onResize.subscribe(() => {
            this.profitSharePieChart.reload();
        });
    }

    ngOnInit() {
        this.profitSharePieChart.reload();
    }
}
