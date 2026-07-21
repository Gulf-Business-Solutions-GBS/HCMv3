import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { Table, TableModule } from 'primeng/table';
import { HostDashboardServiceProxy, GetExpiringTenantsOutput } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';

import { NgScrollbar } from 'ngx-scrollbar';
import { BusyIfDirective } from '../../../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'app-widget-subscription-expiring-tenants',
    templateUrl: './widget-subscription-expiring-tenants.component.html',
    styleUrls: ['./widget-subscription-expiring-tenants.component.css'],
    imports: [NgScrollbar, BusyIfDirective, TableModule, LocalizePipe],
})
export class WidgetSubscriptionExpiringTenantsComponent extends WidgetComponentBaseComponent implements OnInit {
    private _hostDashboardServiceProxy = inject(HostDashboardServiceProxy);

    readonly expiringTenantsTable = viewChild<Table>('ExpiringTenantsTable');

    readonly dataLoading = signal<boolean>(true);
    readonly expiringTenantsData = signal<GetExpiringTenantsOutput | undefined>(undefined);

    ngOnInit() {
        this.getData();
    }

    getData() {
        this._hostDashboardServiceProxy.getSubscriptionExpiringTenantsData().subscribe((data) => {
            this.expiringTenantsData.set(data);
            this.dataLoading.set(false);
        });
    }

    gotoAllExpiringTenants(): void {
        const data = this.expiringTenantsData();
        if (data) {
            const url =
                abp.appPath +
                'app/admin/tenants?' +
                'subscriptionEndDateStart=' +
                encodeURIComponent(data.subscriptionEndDateStart.toString()) +
                '&' +
                'subscriptionEndDateEnd=' +
                encodeURIComponent(data.subscriptionEndDateEnd.toString());

            window.open(url);
        }
    }
}
