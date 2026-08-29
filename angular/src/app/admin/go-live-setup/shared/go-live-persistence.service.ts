import { DestroyRef, Injectable, effect, inject, signal } from '@angular/core';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, debounceTime } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { GoLiveStateServiceProxy, SaveGoLiveStepInput } from '@shared/service-proxies/service-proxies';
import { GoLiveStateService } from './go-live-state.service';

export type SaveStatus = 'idle' | 'saving' | 'saved' | 'failed';

/**
 * Keeps the Go-Live Setup draft on the server so a customer can fill it over several days, and so two
 * people owning different steps do not lose each other's work.
 *
 * Saves per step rather than the whole draft: a 2,000-row employee sheet must not be rewritten because
 * somebody answered a question two steps away. The state service marks a step dirty; this drains that
 * set on a debounce.
 */
@Injectable({ providedIn: 'root' })
export class GoLivePersistenceService {
    /** Long enough to coalesce typing, short enough that closing the tab rarely loses an edit. */
    private static readonly DEBOUNCE_MS = 1200;

    private readonly state = inject(GoLiveStateService);
    private readonly proxy = inject(GoLiveStateServiceProxy);
    private readonly destroyRef = inject(DestroyRef);

    private readonly saveRequests = new Subject<void>();
    private readonly statusSignal = signal<SaveStatus>('idle');
    private readonly lastSavedSignal = signal<Date | null>(null);
    private started = false;

    readonly status = this.statusSignal.asReadonly();
    readonly lastSaved = this.lastSavedSignal.asReadonly();

    constructor() {
        this.saveRequests
            .pipe(debounceTime(GoLivePersistenceService.DEBOUNCE_MS), takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.flush());

        // Any dirty step queues a save. The effect only reads the dirty set, so it does not re-run when
        // an unrelated part of the state changes.
        effect(() => {
            if (this.state.dirtySteps().size > 0) {
                this.saveRequests.next();
            }
        });
    }

    /** Hydrates the draft. Called once when the shell loads; repeat calls are ignored. */
    start(): void {
        if (this.started) {
            return;
        }
        this.started = true;

        this.proxy
            .getState()
            .pipe(catchError(() => of(null)))
            .subscribe((result) => {
                if (!result) {
                    return;
                }

                this.state.hydrate(
                    (result.steps ?? []).map((s) => ({ stepId: s.stepId ?? '', payload: s.payload ?? '' }))
                );

                if (result.lastSavedUtc) {
                    this.lastSavedSignal.set(result.lastSavedUtc.toJSDate());
                }
            });
    }

    /** Writes every dirty step now, bypassing the debounce. */
    flush(): void {
        const dirty = [...this.state.dirtySteps()];
        if (dirty.length === 0) {
            return;
        }

        this.statusSignal.set('saving');

        const saves = dirty.map((stepId) => {
            const input = new SaveGoLiveStepInput();
            input.stepId = stepId;
            // A step emptied by the customer saves as null, which deletes the row server-side rather
            // than leaving an orphaned "{}" behind.
            input.payload = this.state.payloadFor(stepId) ?? undefined;
            return this.proxy.saveStep(input);
        });

        forkJoin(saves).subscribe({
            next: () => {
                // Cleared before the next edit can arrive, so a change made mid-flight stays dirty and
                // is picked up by the following pass rather than being silently dropped.
                this.state.clearDirty(dirty);
                this.statusSignal.set('saved');
                this.lastSavedSignal.set(new Date());
            },
            error: () => {
                // Deliberately leaves the steps dirty so the next edit retries them.
                this.statusSignal.set('failed');
            },
        });
    }
}
