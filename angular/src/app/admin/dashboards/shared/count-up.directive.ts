import { Directive, ElementRef, OnDestroy, effect, inject, input } from '@angular/core';

/**
 * Counts a dashboard number up from its previous value (0 on first show) to the new one, so a figure visibly
 * arrives instead of just appearing. Shared by every dashboard.
 *
 *     <span [countUp]="kpis.onLeaveToday"></span>
 *     <span [countUp]="kpis.leaveDays" [countUpDecimals]="1"></span>
 *
 * Respects the operating system's "reduce motion" setting: the number is then written at once.
 */
@Directive({ selector: '[countUp]' })
export class CountUpDirective implements OnDestroy {
    readonly countUp = input<number | null | undefined>(0);
    readonly countUpDecimals = input(0);
    readonly countUpDuration = input(900);

    private readonly _element = inject(ElementRef<HTMLElement>);
    private _frame: number | undefined;
    private _shown = 0;

    constructor() {
        effect(() => this.animateTo(this.countUp() ?? 0, this.countUpDecimals(), this.countUpDuration()));
    }

    ngOnDestroy(): void {
        this.cancel();
    }

    private animateTo(target: number, decimals: number, duration: number): void {
        this.cancel();

        const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
        const from = this._shown;
        if (reduceMotion || duration <= 0 || from === target) {
            this.write(target, decimals);
            return;
        }

        const start = performance.now();
        const step = (now: number) => {
            const t = Math.min(1, (now - start) / duration);
            const eased = 1 - Math.pow(1 - t, 3); // ease-out cubic: fast start, gentle landing
            this.write(from + (target - from) * eased, decimals);
            this._frame = t < 1 ? requestAnimationFrame(step) : undefined;
        };
        this._frame = requestAnimationFrame(step);
    }

    private write(value: number, decimals: number): void {
        this._shown = value;
        this._element.nativeElement.textContent = value.toLocaleString(undefined, {
            minimumFractionDigits: 0,
            maximumFractionDigits: decimals,
        });
    }

    private cancel(): void {
        if (this._frame !== undefined) {
            cancelAnimationFrame(this._frame);
            this._frame = undefined;
        }
    }
}
