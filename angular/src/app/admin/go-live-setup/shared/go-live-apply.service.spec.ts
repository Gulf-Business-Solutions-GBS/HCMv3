import { TestBed } from '@angular/core/testing';
import { Subject, throwError } from 'rxjs';
import { GoLiveApplyResultDto, GoLiveApplyServiceProxy } from '@shared/service-proxies/service-proxies';
import { GoLiveApplyService } from './go-live-apply.service';
import { GoLivePersistenceService } from './go-live-persistence.service';
import { GoLiveStateService } from './go-live-state.service';

/**
 * The gating rule: Apply is only offered after a preview that came back clean. The server enforces
 * nothing here -- it runs the same code either way -- so this is what keeps the button honest.
 */
describe('GoLiveApplyService', () => {
    let service: GoLiveApplyService;
    let preview$: Subject<GoLiveApplyResultDto>;
    let apply$: Subject<GoLiveApplyResultDto>;
    let flushed: number;

    const result = (created: number, failed: number): GoLiveApplyResultDto =>
        ({
            totalCreated: created,
            totalUpdated: 0,
            totalSkipped: 0,
            totalFailed: failed,
            stages: [],
        }) as unknown as GoLiveApplyResultDto;

    beforeEach(() => {
        preview$ = new Subject<GoLiveApplyResultDto>();
        apply$ = new Subject<GoLiveApplyResultDto>();
        flushed = 0;

        TestBed.configureTestingModule({
            providers: [
                {
                    provide: GoLiveApplyServiceProxy,
                    useValue: { preview: () => preview$, apply: () => apply$ },
                },
                { provide: GoLivePersistenceService, useValue: { flush: () => flushed++ } },
            ],
        });

        service = TestBed.inject(GoLiveApplyService);
    });

    it('should start idle with nothing to show', () => {
        expect(service.phase()).toBe('idle');
        expect(service.result()).toBeNull();
        expect(service.canApply()).toBe(false);
    });

    it('should not offer Apply before a preview has been seen', () => {
        expect(service.canApply()).toBe(false);
    });

    it('should offer Apply after a clean preview', () => {
        service.preview();
        expect(service.phase()).toBe('previewing');

        preview$.next(result(3, 0));

        expect(service.phase()).toBe('previewed');
        expect(service.canApply()).toBe(true);
    });

    it('should refuse Apply when the preview found problems', () => {
        service.preview();
        preview$.next(result(2, 1));

        expect(service.canApply()).toBe(false);
        expect(service.hasFailures()).toBe(true);
    });

    it('should flush the autosave before asking the server', () => {
        // Apply reads the saved draft, not what is on screen, so anything still in the debounce has to
        // reach the server first.
        service.preview();

        expect(flushed).toBe(1);
    });

    it('should report busy while a request is in flight', () => {
        service.preview();
        expect(service.busy()).toBe(true);

        preview$.next(result(1, 0));
        expect(service.busy()).toBe(false);
    });

    it('should ignore a second request while one is in flight', () => {
        service.preview();
        service.preview();

        expect(flushed).toBe(1);
    });

    it('should move to applied once the real run returns', () => {
        service.preview();
        preview$.next(result(3, 0));

        service.apply();
        expect(service.phase()).toBe('applying');

        apply$.next(result(3, 0));
        expect(service.phase()).toBe('applied');
    });

    it('should surface a failed request without pretending it succeeded', () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                {
                    provide: GoLiveApplyServiceProxy,
                    useValue: { preview: () => throwError(() => new Error('boom')), apply: () => apply$ },
                },
                { provide: GoLivePersistenceService, useValue: { flush: () => undefined } },
            ],
        });
        const failing = TestBed.inject(GoLiveApplyService);

        failing.preview();

        expect(failing.phase()).toBe('failed');
        expect(failing.error()).not.toBeNull();
        expect(failing.canApply()).toBe(false);
    });

    it('should clear a stale preview on reset, so an edited draft cannot be applied against it', () => {
        service.preview();
        preview$.next(result(3, 0));
        expect(service.canApply()).toBe(true);

        service.reset();

        expect(service.phase()).toBe('idle');
        expect(service.result()).toBeNull();
        expect(service.canApply()).toBe(false);
    });

    it('should withdraw Apply as soon as a step is edited', () => {
        // The preview described the draft as it was. Editing anything makes that description stale, so
        // the customer has to check again rather than apply a plan that never saw the change.
        const state = TestBed.inject(GoLiveStateService);
        service.preview();
        preview$.next(result(3, 0));
        expect(service.canApply()).toBe(true);

        state.setRows('S3.1', [{ code: '1001' }] as never);
        TestBed.tick();

        expect(service.phase()).toBe('idle');
        expect(service.canApply()).toBe(false);
    });
});
