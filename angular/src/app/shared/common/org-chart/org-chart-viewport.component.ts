import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    ElementRef,
    OnInit,
    computed,
    inject,
    input,
    signal,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent } from 'rxjs';
import { OrgChartExportService } from './org-chart-export.service';

/**
 * Pan/zoom/fullscreen shell that every org-chart screen wraps its content in, so the three charts behave
 * identically: same zoom steps, same drag-to-pan, same fullscreen and same exported image.
 *
 * Deliberately has no toolbar of its own -- each screen renders the zoom, fullscreen and export buttons
 * inside its own toolbar alongside its own filters, and drives this component through the public methods
 * below via a template reference.
 */
@Component({
    selector: 'hcm-org-chart-viewport',
    templateUrl: './org-chart-viewport.component.html',
    styleUrls: ['./org-chart-viewport.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrgChartViewportComponent implements OnInit {
    private static readonly MinZoom = 0.4;
    private static readonly MaxZoom = 2;
    private static readonly ZoomStep = 0.1;

    private readonly _hostElement = inject(ElementRef<HTMLElement>);
    private readonly _destroyRef = inject(DestroyRef);
    private readonly _exportService = inject(OrgChartExportService);

    /** File name (without extension) used when the chart is exported as an image. */
    readonly exportFileName = input('org-chart');

    private readonly _canvas = viewChild.required<ElementRef<HTMLElement>>('canvas');

    private readonly _zoom = signal(1);
    private readonly _panX = signal(0);
    private readonly _panY = signal(0);
    private readonly _isFullscreen = signal(false);
    private readonly _isPanning = signal(false);

    private panOriginX = 0;
    private panOriginY = 0;
    private panStartX = 0;
    private panStartY = 0;

    readonly zoomPercent = computed(() => Math.round(this._zoom() * 100));
    readonly isFullscreen = this._isFullscreen.asReadonly();
    readonly isPanning = this._isPanning.asReadonly();

    readonly canvasTransform = computed(
        () => `translate(${this._panX()}px, ${this._panY()}px) scale(${this._zoom()})`
    );

    ngOnInit(): void {
        // Fullscreen can also be left via Esc or the browser chrome, so mirror the real state rather than
        // assuming our own toggle is the only way out of it.
        fromEvent(document, 'fullscreenchange')
            .pipe(takeUntilDestroyed(this._destroyRef))
            .subscribe(() => {
                this._isFullscreen.set(document.fullscreenElement === this.hostElement);
            });
    }

    zoomIn(): void {
        this.setZoom(this._zoom() + OrgChartViewportComponent.ZoomStep);
    }

    zoomOut(): void {
        this.setZoom(this._zoom() - OrgChartViewportComponent.ZoomStep);
    }

    resetZoom(): void {
        this._zoom.set(1);
        this._panX.set(0);
        this._panY.set(0);
    }

    async toggleFullscreen(): Promise<void> {
        if (document.fullscreenElement === this.hostElement) {
            await document.exitFullscreen();
            return;
        }

        await this.hostElement.requestFullscreen();
    }

    /**
     * Rasterises the chart at its natural size -- the current zoom and pan are presentation state and
     * deliberately do not affect the exported image.
     */
    exportAsImage(): Promise<void> {
        return this._exportService.exportElementAsPng(this._canvas().nativeElement, this.exportFileName());
    }

    onWheel(event: WheelEvent): void {
        // Only intercept the wheel when the user is explicitly zooming; a plain wheel must still scroll
        // the page, which is what a reader expects on a tall chart.
        if (!event.ctrlKey) {
            return;
        }

        event.preventDefault();
        this.setZoom(this._zoom() - Math.sign(event.deltaY) * OrgChartViewportComponent.ZoomStep);
    }

    onPointerDown(event: PointerEvent): void {
        // Left button only, and never when the gesture started on a card -- that is a drill-down click.
        if (event.button !== 0 || (event.target as HTMLElement).closest('.org-chart-card')) {
            return;
        }

        this._isPanning.set(true);
        this.panStartX = event.clientX;
        this.panStartY = event.clientY;
        this.panOriginX = this._panX();
        this.panOriginY = this._panY();
        (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
    }

    onPointerMove(event: PointerEvent): void {
        if (!this._isPanning()) {
            return;
        }

        this._panX.set(this.panOriginX + (event.clientX - this.panStartX));
        this._panY.set(this.panOriginY + (event.clientY - this.panStartY));
    }

    onPointerUp(event: PointerEvent): void {
        if (!this._isPanning()) {
            return;
        }

        this._isPanning.set(false);
        (event.currentTarget as HTMLElement).releasePointerCapture(event.pointerId);
    }

    private get hostElement(): HTMLElement {
        return this._hostElement.nativeElement;
    }

    private setZoom(value: number): void {
        const clamped = Math.min(
            OrgChartViewportComponent.MaxZoom,
            Math.max(OrgChartViewportComponent.MinZoom, value)
        );

        // Guards against floating-point drift accumulating across many steps (1.0000000000000002).
        this._zoom.set(Math.round(clamped * 100) / 100);
    }
}
