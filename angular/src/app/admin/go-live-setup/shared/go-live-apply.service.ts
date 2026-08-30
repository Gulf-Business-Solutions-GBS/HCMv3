import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { GoLiveApplyResultDto, GoLiveApplyServiceProxy } from '@shared/service-proxies/service-proxies';
import { GoLivePersistenceService } from './go-live-persistence.service';
import { GoLiveStateService } from './go-live-state.service';

export type ApplyPhase = 'idle' | 'previewing' | 'previewed' | 'applying' | 'applied' | 'failed';

/**
 * Drives the two-step provisioning the design calls for: see what would happen, then do it.
 *
 * Nothing here decides whether Apply is allowed -- the server runs the same code either way. This just
 * keeps the button honest: a preview must have been seen, and it must have come back clean, before the
 * real run is offered.
 */
@Injectable({ providedIn: 'root' })
export class GoLiveApplyService {
    private readonly proxy = inject(GoLiveApplyServiceProxy);
    private readonly persistence = inject(GoLivePersistenceService);
    private readonly state = inject(GoLiveStateService);

    private readonly phaseSignal = signal<ApplyPhase>('idle');
    private readonly resultSignal = signal<GoLiveApplyResultDto | null>(null);
    private readonly errorSignal = signal<string | null>(null);

    readonly phase = this.phaseSignal.asReadonly();
    readonly result = this.resultSignal.asReadonly();
    readonly error = this.errorSignal.asReadonly();

    readonly busy = computed(() => this.phase() === 'previewing' || this.phase() === 'applying');

    /** Apply is offered only after a preview that found nothing wrong. */
    readonly canApply = computed(() => {
        const result = this.result();
        return this.phase() === 'previewed' && !!result && result.totalFailed === 0;
    });

    readonly hasFailures = computed(() => (this.result()?.totalFailed ?? 0) > 0);

    constructor() {
        // A preview describes the draft as it was when the preview ran. The moment any step is edited
        // that description is stale, so the result is dropped and Apply goes back behind another check.
        // Without this the customer could change a salary and then apply against a preview that never
        // saw it.
        effect(() => {
            if (this.state.dirtySteps().size === 0) {
                return;
            }

            untracked(() => {
                if (this.phase() === 'previewed' || this.phase() === 'applied' || this.phase() === 'failed') {
                    this.reset();
                }
            });
        });
    }

    preview(): void {
        this.run(false);
    }

    apply(): void {
        this.run(true);
    }

    /** Clears the last result, so an edited draft cannot be applied against a stale preview. */
    reset(): void {
        this.phaseSignal.set('idle');
        this.resultSignal.set(null);
        this.errorSignal.set(null);
    }

    private run(apply: boolean): void {
        if (this.busy()) {
            return;
        }

        this.phaseSignal.set(apply ? 'applying' : 'previewing');
        this.errorSignal.set(null);

        // Anything still sitting in the autosave debounce has to reach the server first: Apply reads the
        // saved draft, not what is on screen.
        this.persistence.flush();

        const request = apply ? this.proxy.apply() : this.proxy.preview();

        request.subscribe({
            next: (result) => {
                this.resultSignal.set(result);
                this.phaseSignal.set(apply ? 'applied' : 'previewed');
            },
            error: () => {
                this.phaseSignal.set('failed');
                this.errorSignal.set('failed');
            },
        });
    }
}
