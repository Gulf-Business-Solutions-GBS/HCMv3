import { Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Select } from 'primeng/select';
import { Tooltip } from 'primeng/tooltip';
import { finalize } from 'rxjs/operators';
import { AppConsts } from '@shared/AppConsts';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    Gender,
    GetPositionOrgChartNodeInput,
    PositionOrgChartDto,
    PositionOrgChartNodeDto,
    PositionOrgChartServiceProxy,
    SearchPositionOrgChartNodesInput,
} from '@shared/service-proxies/service-proxies';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { OrgChartCardComponent } from '../../../shared/common/org-chart/org-chart-card.component';
import { OrgChartViewportComponent } from '../../../shared/common/org-chart/org-chart-viewport.component';
import { OrgChartCard } from '../../../shared/common/org-chart/org-chart.models';

/**
 * Position Org Chart -- the third of three org-chart screens, sharing the card and viewport components
 * under `shared/common/org-chart` with the other two.
 *
 * Same three-row shape as the Employee Org Chart: parent position above, focus in the middle, direct
 * children below, moving by re-centring. What differs is what a card stands for -- a position, titled
 * with whoever currently holds it, or labelled vacant when nobody does.
 */
@Component({
    templateUrl: './position-org-chart.component.html',
    styleUrls: ['./position-org-chart.component.less'],
    imports: [
        FormsModule,
        Select,
        Tooltip,
        BusyIfDirective,
        LocalizePipe,
        OrgChartCardComponent,
        OrgChartViewportComponent,
    ],
})
export class PositionOrgChartComponent extends AppComponentBase implements OnInit {
    /** Matches the Employee Org Chart so both charts request the same cached thumbnail. */
    private static readonly PhotoSize = 112;

    private readonly _positionOrgChartService = inject(PositionOrgChartServiceProxy);
    private readonly _router = inject(Router);

    // Optional rather than required: the toolbar reads zoomPercent()/isFullscreen() and sits above the
    // viewport, so the first change-detection pass reads this before the child view exists.
    private readonly viewport = viewChild(OrgChartViewportComponent);

    readonly isLoading = signal(false);
    readonly searchResults = signal<PositionOrgChartNodeDto[]>([]);
    readonly selectedNode = signal<PositionOrgChartNodeDto | null>(null);
    readonly node = signal<PositionOrgChartDto | null>(null);

    /** The selection folded back in, so PrimeNG can still find it after a fresh search replaced the list. */
    readonly searchOptions = computed<PositionOrgChartNodeDto[]>(() => {
        const selected = this.selectedNode();
        const results = this.searchResults();

        if (!selected || results.some((result) => result.id === selected.id)) {
            return results;
        }

        return [selected, ...results];
    });

    readonly focusCard = computed<OrgChartCard | null>(() => {
        const focus = this.node()?.focus;
        return focus ? this.toCard(focus, false) : null;
    });

    readonly parentCard = computed<OrgChartCard | null>(() => {
        const parent = this.node()?.parent;
        return parent ? this.toCard(parent, true) : null;
    });

    readonly childCards = computed<OrgChartCard[]>(() =>
        (this.node()?.children ?? []).map((child) => this.toCard(child, true))
    );

    readonly zoomPercent = computed(() => this.viewport()?.zoomPercent() ?? 100);
    readonly isFullscreen = computed(() => this.viewport()?.isFullscreen() ?? false);

    readonly canGoUp = computed(() => !!this.node()?.parent);

    /** Down follows the deepest branch, matching the Employee Org Chart's arrow. */
    readonly descendTarget = computed<PositionOrgChartNodeDto | null>(() => {
        const children = this.node()?.children ?? [];
        if (!children.length) {
            return null;
        }

        return children.reduce((deepest, candidate) =>
            candidate.totalChildCount > deepest.totalChildCount ? candidate : deepest
        );
    });

    ngOnInit(): void {
        this.loadNode(null);
    }

    get isRtl(): boolean {
        return abp.localization.currentLanguage.name?.startsWith('ar') ?? false;
    }

    loadNode(positionId: number | null): void {
        const input = new GetPositionOrgChartNodeInput();
        input.positionId = positionId ?? undefined;

        this.isLoading.set(true);
        this._positionOrgChartService
            .getNode(input)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe((result) => {
                this.node.set(result);
                this.selectedNode.set(result.focus ?? null);
            });
    }

    searchNodes(filter: string): void {
        const input = new SearchPositionOrgChartNodesInput();
        input.filter = filter;
        input.maxResultCount = 50;

        this._positionOrgChartService
            .searchNodes(input)
            .subscribe((results) => this.searchResults.set(results ?? []));
    }

    onSearchFilter(event: { filter: string }): void {
        this.searchNodes(event.filter);
    }

    onSearchSelect(positionId: number | null): void {
        if (!positionId) {
            return;
        }

        const picked = this.searchOptions().find((option) => option.id === positionId);
        if (picked) {
            this.selectedNode.set(picked);
        }

        this.loadNode(positionId);
    }

    onCardClick(card: OrgChartCard): void {
        this.loadNode(card.id);
    }

    /**
     * The action button opens the HOLDER's employee record, not the position -- the card id is a position
     * id, so the employee id is carried separately and the button is hidden when the post is vacant.
     */
    onOpenHolder(card: OrgChartCard): void {
        const holderEmployeeId = this.holderEmployeeIdOf(card.id);
        if (holderEmployeeId) {
            this._router.navigate(['/app/admin/employees', holderEmployeeId]);
        }
    }

    positionName(node: PositionOrgChartNodeDto): string {
        return (this.isRtl ? node.nameAr || node.nameEn : node.nameEn) ?? '';
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

    goUp(): void {
        const parent = this.node()?.parent;
        if (parent) {
            this.loadNode(parent.id);
        }
    }

    goDown(): void {
        const target = this.descendTarget();
        if (target) {
            this.loadNode(target.id);
        }
    }

    exportAsImage(): void {
        this.viewport()
            ?.exportAsImage()
            .catch(() => this.message.error(this.l('OrgChartExportFailed')));
    }

    /** Looks the holder up across whichever of the three rows the card came from. */
    private holderEmployeeIdOf(positionId: number): number | undefined {
        const chart = this.node();
        if (!chart) {
            return undefined;
        }

        const candidates = [chart.focus, chart.parent, ...(chart.children ?? [])];
        return candidates.find((candidate) => candidate?.id === positionId)?.holderEmployeeId;
    }

    private toCard(node: PositionOrgChartNodeDto, navigable: boolean): OrgChartCard {
        const holderName = this.isRtl
            ? node.holderFullNameAr || node.holderFullNameEn
            : node.holderFullNameEn;

        return {
            id: node.id,
            variant: 'person',
            // The position is the heading and its holder is the card's title -- a chart of posts is only
            // readable when you can see who is actually in them.
            typeLabel: this.positionName(node),
            code: node.code ?? null,
            name: node.isVacant ? this.l('Vacant') : (holderName ?? this.l('Vacant')),
            accentKey: null,
            avatar: {
                imageUrl: node.holderHasPhoto && node.holderEmployeeId
                    ? this.buildPhotoUrl(node.holderEmployeeId)
                    : null,
                // A vacant post gets the neutral figure: there is no gender to represent, and showing one
                // would imply somebody is in the role.
                fallback: node.isVacant
                    ? 'neutral'
                    : node.holderGender === Gender.Female
                      ? 'female'
                      : node.holderGender === Gender.Male
                        ? 'male'
                        : 'neutral',
            },
            // Hidden when vacant (no record to open) and for anyone without rights to employee records.
            action:
                !node.isVacant && this.isGranted('Pages.PersonnelAdministration.Employees')
                    ? { tooltip: this.l('ViewEmployeeRecord') }
                    : undefined,
            metrics: node.totalChildCount
                ? [
                      { icon: 'person', value: node.directChildCount, tooltip: this.l('DirectPositions') },
                      { icon: 'people', value: node.totalChildCount, tooltip: this.l('TotalPositions') },
                  ]
                : [],
            stripCount: null,
            navigable,
        };
    }

    private buildPhotoUrl(employeeId: number): string {
        return `${AppConsts.remoteServiceBaseUrl}/api/employee-photo/${employeeId}?size=${PositionOrgChartComponent.PhotoSize}`;
    }
}
