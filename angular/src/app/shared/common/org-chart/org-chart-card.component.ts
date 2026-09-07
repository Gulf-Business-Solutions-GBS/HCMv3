import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Tooltip } from 'primeng/tooltip';
import { OrgChartCard, resolveOrgChartAccent } from './org-chart.models';

/**
 * A single node card, shared by all three org-chart screens so that the Company Structure, Employee and
 * Position charts are visually identical by construction rather than by convention.
 *
 * Presentational only: it renders an <see cref="OrgChartCard"/> and emits a click. Everything about what
 * the numbers mean, and what happens on click, belongs to the screen that hosts it.
 */
@Component({
    selector: 'hcm-org-chart-card',
    templateUrl: './org-chart-card.component.html',
    styleUrls: ['./org-chart-card.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Tooltip],
})
export class OrgChartCardComponent {
    readonly card = input.required<OrgChartCard>();

    /** Draws the card in its "current focus" treatment: larger, raised, no hover affordance. */
    readonly focused = input(false);

    readonly cardClick = output<OrgChartCard>();

    readonly accentColor = computed(() => resolveOrgChartAccent(this.card().accentKey));

    onClick(): void {
        const card = this.card();
        if (card.navigable) {
            this.cardClick.emit(card);
        }
    }
}
