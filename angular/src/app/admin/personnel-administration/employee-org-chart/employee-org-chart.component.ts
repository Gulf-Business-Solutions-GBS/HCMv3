import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Select } from 'primeng/select';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    EmployeeOrgChartDto,
    EmployeeOrgChartNodeDto,
    EmployeeOrgChartServiceProxy,
    Gender,
    GetEmployeeOrgChartNodeInput,
    SearchEmployeeOrgChartNodesInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { OrgChartCardComponent } from '../../../shared/common/org-chart/org-chart-card.component';
import { OrgChartViewportComponent } from '../../../shared/common/org-chart/org-chart-viewport.component';
import { OrgChartCard } from '../../../shared/common/org-chart/org-chart.models';

/**
 * Employee Org Chart -- the second of three org-chart screens, sharing the card and viewport components
 * under `shared/common/org-chart` with the Company Structure Overview.
 *
 * Three rows and no more: the immediate manager above, the person in focus, and their direct reports as
 * a wrapped grid below. Clicking any card re-centres the chart on that person, and the up/down arrows do
 * the same one level at a time. Deliberately not a whole-tree render -- at several thousand employees
 * that is unreadable as well as slow.
 */
@Component({
    templateUrl: './employee-org-chart.component.html',
    styleUrls: ['./employee-org-chart.component.less'],
    animations: [appModuleAnimation()],
    imports: [
        FormsModule,
        Select,
        Tooltip,
        SubHeaderComponent,
        BusyIfDirective,
        LocalizePipe,
        OrgChartCardComponent,
        OrgChartViewportComponent,
    ],
})
export class EmployeeOrgChartComponent extends AppComponentBase implements OnInit {
    /**
     * Avatar edge in CSS pixels is 56; asking for 112 keeps it sharp on a 2x display while staying a small
     * fraction of the stored photo. The endpoint caps and centre-crops server-side.
     */
    private static readonly PhotoSize = 112;

    private readonly _employeeOrgChartService = inject(EmployeeOrgChartServiceProxy);
    private readonly _router = inject(Router);

    // Intentionally not viewChild.required: the toolbar reads zoomPercent()/isFullscreen() and sits above
    // the viewport in the template, so the first change-detection pass reads this before the child view
    // exists. A required query throws there; an optional one falls back to the defaults below.
    private readonly viewport = viewChild(OrgChartViewportComponent);

    readonly isLoading = signal(false);
    readonly searchResults = signal<EmployeeOrgChartNodeDto[]>([]);

    /**
     * The person currently picked in the search box, held separately from the results list. PrimeNG can
     * only render a label for an option it can still find, and the results list is replaced on every
     * keystroke -- without this the box would fall back to stringifying the value.
     */
    readonly selectedNode = signal<EmployeeOrgChartNodeDto | null>(null);

    /**
     * The search box's options, with the current selection folded in when a fresh search has dropped it.
     * PrimeNG resolves the displayed label by looking the bound value up in this list, so a selection that
     * is not present renders as the raw object.
     */
    readonly searchOptions = computed<EmployeeOrgChartNodeDto[]>(() => {
        const selected = this.selectedNode();
        const results = this.searchResults();

        if (!selected || results.some((result) => result.id === selected.id)) {
            return results;
        }

        return [selected, ...results];
    });
    readonly node = signal<EmployeeOrgChartDto | null>(null);

    readonly focusCard = computed<OrgChartCard | null>(() => {
        const focus = this.node()?.focus;
        return focus ? this.toCard(focus, false) : null;
    });

    readonly managerCard = computed<OrgChartCard | null>(() => {
        const manager = this.node()?.manager;
        return manager ? this.toCard(manager, true) : null;
    });

    readonly directReportCards = computed<OrgChartCard[]>(() =>
        (this.node()?.directReports ?? []).map((report) => this.toCard(report, true))
    );

    readonly zoomPercent = computed(() => this.viewport()?.zoomPercent() ?? 100);
    readonly isFullscreen = computed(() => this.viewport()?.isFullscreen() ?? false);

    /** The up arrow is only meaningful when somebody sits above the person in focus. */
    readonly canGoUp = computed(() => !!this.node()?.manager);

    /**
     * Where the down arrow leads: the direct report with the most people beneath them.
     *
     * The chart shows one level down at a time, so "descend" has to pick somebody. Following the deepest
     * branch walks the main line of the hierarchy, which is what the arrow is for -- reaching any other
     * individual is a single click on their own card.
     */
    readonly descendTarget = computed<EmployeeOrgChartNodeDto | null>(() => {
        const reports = this.node()?.directReports ?? [];
        if (!reports.length) {
            return null;
        }

        return reports.reduce((deepest, candidate) =>
            candidate.totalReportCount > deepest.totalReportCount ? candidate : deepest
        );
    });

    ngOnInit(): void {
        this.loadNode(null);
    }

    /** Arabic names are shown wherever the UI language is Arabic, mirroring the rest of the app. */
    get isRtl(): boolean {
        return abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    }

    loadNode(employeeId: number | null): void {
        const input = new GetEmployeeOrgChartNodeInput();
        input.employeeId = employeeId ?? undefined;

        this.isLoading.set(true);
        this._employeeOrgChartService
            .getNode(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => {
                this.node.set(result);
                // Keep the search box showing whoever the chart is centred on, however they got there --
                // a card click and a search both land here.
                this.selectedNode.set(result.focus ?? null);
            });
    }

    searchNodes(filter: string): void {
        const input = new SearchEmployeeOrgChartNodesInput();
        input.filter = filter;
        input.maxResultCount = 50;

        this._employeeOrgChartService
            .searchNodes(input)
            .subscribe((results) => this.searchResults.set(results ?? []));
    }

    onSearchFilter(event: { filter: string }): void {
        this.searchNodes(event.filter);
    }

    /** Jumping straight to someone found by name, rather than walking the reporting lines to them. */
    onSearchSelect(employeeId: number | null): void {
        if (!employeeId) {
            return;
        }

        const picked = this.searchOptions().find((option) => option.id === employeeId);
        if (picked) {
            this.selectedNode.set(picked);
        }

        this.loadNode(employeeId);
    }

    onCardClick(card: OrgChartCard): void {
        this.loadNode(card.id);
    }

    /** Up arrow: re-centre on the manager, which brings their own manager into view above. */
    goUp(): void {
        const manager = this.node()?.manager;
        if (manager) {
            this.loadNode(manager.id);
        }
    }

    /** Down arrow: follow the deepest branch one level down. */
    goDown(): void {
        const target = this.descendTarget();
        if (target) {
            this.loadNode(target.id);
        }
    }

    onOpenEmployee(card: OrgChartCard): void {
        this._router.navigate(['/app/admin/employees', card.id]);
    }

    displayName(node: EmployeeOrgChartNodeDto): string {
        return (this.isRtl ? node.fullNameAr || node.fullNameEn : node.fullNameEn) ?? '';
    }

    zoomIn(): void {
        this.viewport()?.zoomIn();
    }

    zoomOut(): void {
        this.viewport()?.zoomOut();
    }

    resetZoom(): void {
        this.viewport()?.resetZoom();
    }

    toggleFullscreen(): void {
        void this.viewport()?.toggleFullscreen();
    }

    exportAsImage(): void {
        this.viewport()
            ?.exportAsImage()
            .catch(() => this.message.error(this.l('OrgChartExportFailed')));
    }

    private toCard(node: EmployeeOrgChartNodeDto, navigable: boolean): OrgChartCard {
        const jobTitle = this.isRtl ? node.jobTitleAr || node.jobTitleEn : node.jobTitleEn;

        return {
            id: node.id,
            variant: 'person',
            typeLabel: jobTitle ?? null,
            code: node.code ?? null,
            name: this.displayName(node),
            accentKey: null,
            avatar: {
                // Only ask for a photo when the server says one exists -- otherwise every person without
                // one costs a wasted request that comes back 404.
                imageUrl: node.hasPhoto ? this.buildPhotoUrl(node.id) : null,
                // Gender is a required enum on Employee, so there is always a silhouette to fall back to.
                // Anything outside the two known values lands on the neutral figure rather than guessing.
                fallback:
                    node.gender === Gender.Female
                        ? 'female'
                        : node.gender === Gender.Male
                          ? 'male'
                          : 'neutral',
            },
            // Hidden entirely for anyone who cannot open employee records, rather than shown and then
            // failing on the route guard.
            action: this.isGranted('Pages.PersonnelAdministration.Employees')
                ? { tooltip: this.l('ViewEmployeeRecord') }
                : undefined,
            // Someone with nobody under them shows no figures at all, matching the reference design --
            // "0 / 0" is noise on the majority of cards.
            metrics: node.totalReportCount
                ? [
                      { icon: 'person', value: node.directReportCount, tooltip: this.l('DirectReports') },
                      { icon: 'people', value: node.totalReportCount, tooltip: this.l('TotalReports') },
                  ]
                : [],
            stripCount: null,
            navigable,
        };
    }

    private buildPhotoUrl(employeeId: number): string {
        return `${AppConsts.remoteServiceBaseUrl}/api/employee-photo/${employeeId}?size=${EmployeeOrgChartComponent.PhotoSize}`;
    }
}
