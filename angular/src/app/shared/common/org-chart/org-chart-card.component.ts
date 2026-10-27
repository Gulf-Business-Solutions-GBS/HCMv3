import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { Tooltip } from 'primeng/tooltip';
import { OrgChartCard, resolveOrgChartAccent } from './org-chart.models';

/**
 * A single node card, shared by all three org-chart screens so that the Company Structure, Employee and
 * Position charts are visually identical by construction rather than by convention.
 *
 * Presentational only: it renders an `OrgChartCard` and emits clicks. What the numbers mean, and what
 * happens on click, belongs to the screen that hosts it.
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

    /** The small trailing button on a person card -- the host decides what it opens. */
    readonly actionClick = output<OrgChartCard>();

    /**
     * Id of the card whose avatar failed to load, so the silhouette takes over -- a 404 from the photo
     * endpoint is the ordinary case for someone with no photo. Stored as an id rather than a flag because
     * Angular reuses a component instance when the bound card changes, and a boolean would leave the next
     * person stuck on the silhouette.
     */
    private readonly failedImageCardId = signal<number | null>(null);

    readonly accentColor = computed(() => resolveOrgChartAccent(this.card().accentKey));

    readonly isPerson = computed(() => this.card().variant === 'person');

    readonly showPhoto = computed(() => {
        const card = this.card();
        return !!card.avatar?.imageUrl && this.failedImageCardId() !== card.id;
    });

    onClick(): void {
        const card = this.card();
        if (card.navigable) {
            this.cardClick.emit(card);
        }
    }

    onActionClick(event: MouseEvent): void {
        // The button sits inside the clickable card, so without this the host would also re-centre the
        // chart on the very person whose record was just opened.
        event.stopPropagation();
        this.actionClick.emit(this.card());
    }

    onImageError(): void {
        this.failedImageCardId.set(this.card().id);
    }
}
