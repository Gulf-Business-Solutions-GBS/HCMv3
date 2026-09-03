import { Component, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { QuickLinksComponent } from './quick-links/quick-links.component';
import { MySummaryComponent } from './my-summary/my-summary.component';
import { EssHomeComponent } from '../ess/ess-home.component';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [QuickLinksComponent, MySummaryComponent, EssHomeComponent],
})
export class DashboardComponent extends AppComponentBase {
}
