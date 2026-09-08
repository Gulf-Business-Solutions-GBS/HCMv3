import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DateTime } from 'luxon';
import { Select } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
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
import { PrimengDatePickerLuxonModifierDirective } from '../../../../shared/utils/date-time/primeng-date-picker-luxon-modifier.directive';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { OrgChartCardComponent } from '../../../shared/common/org-chart/org-chart-card.component';
import { OrgChartViewportComponent } from '../../../shared/common/org-chart/org-chart-viewport.component';
import { OrgChartCard } from '../../../shared/common/org-chart/org-chart.models';

/**
 * Employee Org Chart -- the second of three org-chart screens, sharing the card and viewport components
 * under `shared/common/org-chart` with the Company Structure Overview.
 *
 * Reads as the reference design does: the management chain stacked above the person in focus, their
 * direct reports as a wrapped grid below. Clicking a report re-centres on them; the chain above is
 * clickable to walk back up. Only one level loads at a time, which is what makes it usable across
 * thousands of employees.
 */
@Component({
    templateUrl: './employee-org-chart.component.html',
    styleUrls: ['./employee-org-chart.component.less'],
    animations: [appModuleAnimation()],
    imports: [
        FormsModule,
        Select,
        DatePickerModule,
        Tooltip,
        SubHeaderComponent,
        BusyIfDirective,
        LocalizePipe,
        PrimengDatePickerLuxonModifierDirective,
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
    readonly asOfDate = signal<DateTime>(DateTime.local().startOf('day'));
    readonly node = signal<EmployeeOrgChartDto | null>(null);

    readonly focusCard = computed<OrgChartCard | null>(() => {
        const focus = this.node()?.focus;
        return focus ? this.toCard(focus, false) : null;
    });

    readonly managerChainCards = computed<OrgChartCard[]>(() =>
        (this.node()?.managerChain ?? []).map((manager) => this.toCard(manager, true))
    );

    readonly directReportCards = computed<OrgChartCard[]>(() =>
        (this.node()?.directReports ?? []).map((report) => this.toCard(report, true))
    );

    readonly zoomPercent = computed(() => this.viewport()?.zoomPercent() ?? 100);
    readonly isFullscreen = computed(() => this.viewport()?.isFullscreen() ?? false);

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
        input.asOfDate = this.asOfDate();

        this.isLoading.set(true);
        this._employeeOrgChartService
            .getNode(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => this.node.set(result));
    }

    onAsOfDateChange(date: DateTime | null): void {
        // The date changes the reporting lines and titles, not who is in view, so stay on the same person.
        this.asOfDate.set(date ?? DateTime.local().startOf('day'));
        this.loadNode(this.node()?.focus?.id ?? null);
    }

    searchNodes(filter: string): void {
        const input = new SearchEmployeeOrgChartNodesInput();
        input.filter = filter;
        input.asOfDate = this.asOfDate();
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
        if (employeeId) {
            this.loadNode(employeeId);
        }
    }

    onCardClick(card: OrgChartCard): void {
        this.loadNode(card.id);
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
            metricsTooltip: `${this.l('DirectReports')} / ${this.l('TotalReports')}`,
            stripCount: null,
            navigable,
        };
    }

    private buildPhotoUrl(employeeId: number): string {
        return `${AppConsts.remoteServiceBaseUrl}/api/employee-photo/${employeeId}?size=${EmployeeOrgChartComponent.PhotoSize}`;
    }
}
