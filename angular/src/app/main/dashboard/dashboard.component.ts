import { Component, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardCustomizationConst } from '@app/shared/common/customizable-dashboard/DashboardCustomizationConsts';
import { CustomizableDashboardComponent } from '../../shared/common/customizable-dashboard/customizable-dashboard.component';
import { QuickLinksComponent } from './quick-links/quick-links.component';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [CustomizableDashboardComponent, QuickLinksComponent],
})
export class DashboardComponent extends AppComponentBase {
    dashboardName = DashboardCustomizationConst.dashboardNames.defaultTenantDashboard;
}
