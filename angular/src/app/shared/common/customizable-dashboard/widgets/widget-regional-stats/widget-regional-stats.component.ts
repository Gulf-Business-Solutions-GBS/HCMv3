import { Component, OnInit, inject, signal } from '@angular/core';
import { DashboardChartBase } from '../dashboard-chart-base';
import { TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { curveBasis } from 'd3-shape';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { NgScrollbar } from 'ngx-scrollbar';
import { CurrencyPipe } from '@angular/common';
import { LineChartModule } from '@swimlane/ngx-charts';

class RegionalStatsTable extends DashboardChartBase {
    readonly stats = signal<any[]>([]);
    colors = ['#00c5dc', '#f4516c', '#34bfa3', '#ffb822'];
    customColors = [
        { name: '1', value: '#00c5dc' },
        { name: '2', value: '#f4516c' },
        { name: '3', value: '#34bfa3' },
        { name: '4', value: '#ffb822' },
        { name: '5', value: '#00c5dc' },
    ];

    curve: any = curveBasis;

    constructor(private _dashboardService: TenantDashboardServiceProxy) {
        super();
    }

    init() {
        this.reload();
    }

    formatData(statsData: any[]): any[] {
        for (let j = 0; j < statsData.length; j++) {
            let stat = statsData[j];

            let series = [];
            for (let i = 0; i < stat.change.length; i++) {
                series.push({
                    name: i + 1,
                    value: stat.change[i],
                });
            }

            stat.changeData = [
                {
                    name: j + 1,
                    series: series,
                },
            ];
        }
        return statsData;
    }

    reload() {
        this.showLoading();
        this._dashboardService.getRegionalStats().subscribe((result) => {
            this.stats.set(this.formatData(result.stats));
            this.hideLoading();
        });
    }
}

@Component({
    selector: 'app-widget-regional-stats',
    templateUrl: './widget-regional-stats.component.html',
    styleUrls: ['./widget-regional-stats.component.css'],
    imports: [NgScrollbar, LineChartModule, CurrencyPipe],
})
export class WidgetRegionalStatsComponent extends WidgetComponentBaseComponent implements OnInit {
    private _dashboardService = inject(TenantDashboardServiceProxy);

    regionalStatsTable: RegionalStatsTable;

    constructor() {
        super();
        this.regionalStatsTable = new RegionalStatsTable(this._dashboardService);
    }

    ngOnInit() {
        this.regionalStatsTable.init();
    }
}
