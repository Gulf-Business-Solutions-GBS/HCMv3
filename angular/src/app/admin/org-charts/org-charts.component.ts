import { Component, OnInit, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

interface OrgChartTab {
    /** Child route segment under /app/admin/org-charts. */
    path: string;
    /** Localization key for the tab label. */
    labelKey: string;
    permission: string;
}

/**
 * Shell for the three org charts, holding them as tabs on one page.
 *
 * Routed tabs rather than one component switching internal state, which keeps four things that matter:
 * every chart stays deep-linkable and bookmarkable, each keeps its own permission (a user may hold one
 * and not the others), each stays lazily loaded so opening the page pulls only the chart being viewed,
 * and the Quick Nav catalogue keeps pointing at real routes.
 *
 * The shell owns the page heading; the charts themselves render only their toolbar and canvas, so the
 * title is not repeated under the tab that already names it.
 */
@Component({
    templateUrl: './org-charts.component.html',
    styleUrls: ['./org-charts.component.less'],
    animations: [appModuleAnimation()],
    imports: [RouterOutlet, RouterLink, RouterLinkActive, SubHeaderComponent, LocalizePipe],
})
export class OrgChartsComponent extends AppComponentBase implements OnInit {
    private static readonly BasePath = '/app/admin/org-charts';

    private readonly _router = inject(Router);

    private readonly allTabs: OrgChartTab[] = [
        {
            path: 'company',
            labelKey: 'CompanyStructureOverview',
            permission: 'Pages.OrganizationManagement.CompanyStructureChart',
        },
        {
            path: 'employee',
            labelKey: 'EmployeeOrgChart',
            permission: 'Pages.PersonnelAdministration.EmployeeOrgChart',
        },
        {
            path: 'position',
            labelKey: 'PositionOrgChart',
            permission: 'Pages.ManpowerPlanningManagement.PositionOrgChart',
        },
    ];

    /** Only the charts this user may open. A single-tab bar still renders -- it says where you are. */
    readonly tabs = computed(() => this.allTabs.filter((tab) => this.isGranted(tab.permission)));

    ngOnInit(): void {
        this.redirectToFirstPermittedTab();
    }

    /**
     * Sends /org-charts on to a real tab.
     *
     * Done here rather than with a `redirectTo` on the empty child route because the right destination
     * depends on permissions: a fixed redirect would bounce anyone without that particular chart straight
     * back out through the route guard, even though another tab was open to them.
     */
    private redirectToFirstPermittedTab(): void {
        const url = this._router.url.split('?')[0].replace(/\/$/, '');
        if (url !== OrgChartsComponent.BasePath) {
            return;
        }

        const firstTab = this.tabs()[0];
        if (firstTab) {
            void this._router.navigate([OrgChartsComponent.BasePath, firstTab.path], { replaceUrl: true });
        }
    }
}
