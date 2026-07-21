import { Component, inject, viewChild, signal } from '@angular/core';
import { Table, TableModule } from 'primeng/table';
import { HostDashboardServiceProxy, GetRecentTenantsOutput } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';

import { NgScrollbarModule } from 'ngx-scrollbar';
import { BusyIfDirective } from '../../../../../../shared/utils/busy-if.directive';
import { LuxonFormatPipe } from '../../../../../../shared/utils/luxon-format.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'app-widget-recent-tenants',
    templateUrl: './widget-recent-tenants.component.html',
    styleUrls: ['./widget-recent-tenants.component.css'],
    imports: [BusyIfDirective, NgScrollbarModule, TableModule, LuxonFormatPipe, LocalizePipe],
})
export class WidgetRecentTenantsComponent extends WidgetComponentBaseComponent {
    private _hostDashboardServiceProxy = inject(HostDashboardServiceProxy);

    readonly recentTenantsTable = viewChild<Table>('RecentTenantsTable');

    readonly loading = signal(true);
    readonly recentTenantsData = signal<GetRecentTenantsOutput | undefined>(undefined);

    constructor() {
        super();
        this.loadRecentTenantsData();
    }

    loadRecentTenantsData() {
        this._hostDashboardServiceProxy.getRecentTenantsData().subscribe((data) => {
            this.recentTenantsData.set(data);
            this.loading.set(false);
        });
    }

    gotoAllRecentTenants(): void {
        const data = this.recentTenantsData();
        if (data) {
            window.open(
                abp.appPath +
                    'app/admin/tenants?' +
                    'creationDateStart=' +
                    encodeURIComponent(data.tenantCreationStartDate.toString())
            );
        }
    }
}
