import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DateTime } from 'luxon';
import { Select } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    CompanyStructureChartServiceProxy,
    CompanyStructureNodeDto,
    GetCompanyStructureNodeInput,
    OrgChartNodeDto,
    OrgChartNodeSummaryDto,
    OrgChartTypeOptionDto,
    SearchCompanyStructureNodesInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { PrimengDatePickerLuxonModifierDirective } from '../../../../shared/utils/date-time/primeng-date-picker-luxon-modifier.directive';
import { OrgChartCardComponent } from '../../../shared/common/org-chart/org-chart-card.component';
import { OrgChartViewportComponent } from '../../../shared/common/org-chart/org-chart-viewport.component';
import { OrgChartCard } from '../../../shared/common/org-chart/org-chart.models';

/**
 * Company Structure Overview -- the first of three org-chart screens, all of which share the card and
 * viewport components under <c>shared/common/org-chart</c>.
 *
 * This one is a drill-down explorer rather than a whole-tree render: it shows the focus unit with its
 * immediate children as a wrapped card grid and descends when a child is clicked, which keeps it usable
 * on a tenant whose tree is far too wide to draw at once. The breadcrumb above the focus card walks back
 * up. Every headcount reflects the "As Of" date in the toolbar.
 */
@Component({
    templateUrl: './company-structure-chart.component.html',
    styleUrls: ['./company-structure-chart.component.less'],
    imports: [
        FormsModule,
        Select,
        DatePickerModule,
        Tooltip,
        BusyIfDirective,
        LocalizePipe,
        PrimengDatePickerLuxonModifierDirective,
        OrgChartCardComponent,
        OrgChartViewportComponent,
    ],
})
export class CompanyStructureChartComponent extends AppComponentBase implements OnInit {
    private readonly _companyStructureChartService = inject(CompanyStructureChartServiceProxy);

    // Intentionally not viewChild.required: the toolbar reads zoomPercent()/isFullscreen() and sits above
    // the viewport in the template, so the first change-detection pass reads this before the child view
    // exists. A required query throws there; an optional one simply falls back to the defaults below.
    private readonly viewport = viewChild(OrgChartViewportComponent);

    readonly isLoading = signal(false);
    readonly roots = signal<OrgChartNodeSummaryDto[]>([]);
    readonly organizationUnitTypes = signal<OrgChartTypeOptionDto[]>([]);
    readonly searchResults = signal<OrgChartNodeSummaryDto[]>([]);

    readonly selectedRootId = signal<number | null>(null);
    readonly selectedTypeId = signal<number | null>(null);
    readonly asOfDate = signal<DateTime>(DateTime.local().startOf('day'));

    readonly node = signal<CompanyStructureNodeDto | null>(null);

    readonly focusCard = computed<OrgChartCard | null>(() => {
        const focus = this.node()?.focus;
        return focus ? this.toCard(focus, false) : null;
    });

    readonly childCards = computed<OrgChartCard[]>(() =>
        (this.node()?.children ?? []).map((child) => this.toCard(child, true))
    );

    readonly ancestors = computed<OrgChartNodeSummaryDto[]>(() => this.node()?.ancestors ?? []);

    readonly zoomPercent = computed(() => this.viewport()?.zoomPercent() ?? 100);
    readonly isFullscreen = computed(() => this.viewport()?.isFullscreen() ?? false);

    ngOnInit(): void {
        this.loadFilterOptions();
    }

    /** Arabic names are shown wherever the UI language is Arabic, mirroring the rest of the app. */
    get isRtl(): boolean {
        return abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    }

    loadFilterOptions(): void {
        this._companyStructureChartService.getFilterOptions().subscribe((options) => {
            this.roots.set(options.roots ?? []);
            this.organizationUnitTypes.set(options.organizationUnitTypes ?? []);

            const firstRoot = this.roots()[0];
            if (firstRoot) {
                this.selectedRootId.set(firstRoot.id);
            }

            this.loadNode(this.selectedRootId());
        });
    }

    loadNode(organizationUnitId: number | null): void {
        const input = new GetCompanyStructureNodeInput();
        input.organizationUnitId = organizationUnitId ?? undefined;
        input.asOfDate = this.asOfDate();

        this.isLoading.set(true);
        this._companyStructureChartService
            .getNode(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => this.node.set(result));
    }

    onRootChange(rootId: number | null): void {
        this.selectedRootId.set(rootId);
        this.searchResults.set([]);
        this.loadNode(rootId);
    }

    onAsOfDateChange(date: DateTime | null): void {
        // The date only changes the figures, not which node is in view, so stay where the user is.
        this.asOfDate.set(date ?? DateTime.local().startOf('day'));
        this.loadNode(this.node()?.focus?.id ?? this.selectedRootId());
    }

    onTypeChange(typeId: number | null): void {
        this.selectedTypeId.set(typeId);
        this.searchNodes('');
    }

    searchNodes(filter: string): void {
        const input = new SearchCompanyStructureNodesInput();
        input.filter = filter;
        input.organizationUnitTypeId = this.selectedTypeId() ?? undefined;
        input.rootOrganizationUnitId = this.selectedRootId() ?? undefined;
        input.maxResultCount = 50;

        this._companyStructureChartService
            .searchNodes(input)
            .subscribe((results) => this.searchResults.set(results ?? []));
    }

    onSearchFilter(event: { filter: string }): void {
        this.searchNodes(event.filter);
    }

    /** Jumping straight to a searched node, rather than drilling down to it level by level. */
    onSearchSelect(organizationUnitId: number | null): void {
        if (organizationUnitId) {
            this.loadNode(organizationUnitId);
        }
    }

    onCardClick(card: OrgChartCard): void {
        this.loadNode(card.id);
    }

    onBreadcrumbClick(organizationUnitId: number): void {
        this.loadNode(organizationUnitId);
    }

    displayName(node: OrgChartNodeDto | OrgChartNodeSummaryDto): string {
        return (this.isRtl ? node.nameAr || node.nameEn : node.nameEn) ?? '';
    }

    typeDisplayName(type: OrgChartTypeOptionDto): string {
        return (this.isRtl ? type.nameAr || type.nameEn : type.nameEn) ?? '';
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

    private toCard(node: OrgChartNodeDto, navigable: boolean): OrgChartCard {
        const typeName = this.isRtl
            ? node.organizationUnitTypeNameAr || node.organizationUnitTypeNameEn
            : node.organizationUnitTypeNameEn;

        return {
            id: node.id,
            typeLabel: typeName ?? null,
            code: node.code ?? null,
            name: this.displayName(node),
            accentKey: node.organizationUnitTypeCode ?? null,
            metrics: [
                {
                    icon: 'person',
                    value: node.directEmployeeCount,
                    tooltip: this.l('DirectEmployees'),
                },
                {
                    icon: 'people',
                    value: node.totalEmployeeCount,
                    tooltip: this.l('TotalEmployees'),
                },
            ],
            // A unit with no children shows no strip rather than a zero, but is still clickable: focusing
            // a leaf is how the user reads its own figures and gets a breadcrumb back up.
            stripCount: node.childUnitCount > 0 ? node.childUnitCount : null,
            stripTooltip: this.l('ChildUnits'),
            navigable,
        };
    }
}
