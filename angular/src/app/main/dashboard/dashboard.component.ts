import { Component, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { QuickLinksComponent } from './quick-links/quick-links.component';
import { MySummaryComponent } from './my-summary/my-summary.component';
import { EssHomeComponent } from '../ess/ess-home.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.less'],
    encapsulation: ViewEncapsulation.None,
    imports: [QuickLinksComponent, MySummaryComponent, EssHomeComponent, SubHeaderComponent, LocalizePipe],
})
export class DashboardComponent extends AppComponentBase {
}
