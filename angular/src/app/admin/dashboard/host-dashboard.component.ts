import { Component, ViewEncapsulation, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardCustomizationConst } from '@app/shared/common/customizable-dashboard/DashboardCustomizationConsts';
import { CustomizableDashboardComponent } from '../../shared/common/customizable-dashboard/customizable-dashboard.component';

@Component({
    selector: 'app-host-dashboard',
    templateUrl: './host-dashboard.component.html',
    styleUrls: ['./host-dashboard.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [CustomizableDashboardComponent],
})
export class HostDashboardComponent extends AppComponentBase {
    readonly dashboardName = signal(DashboardCustomizationConst.dashboardNames.defaultHostDashboard);
}
