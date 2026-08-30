import { TestBed } from '@angular/core/testing';
import { GoLiveStateService } from './go-live-state.service';
import { GO_LIVE_STEPS, findStepById, nextStep, previousStep } from './go-live-registry';
import { PasteGridRow } from './paste-grid/paste-grid.types';

describe('go-live registry', () => {
    it('should give every step a unique id and path', () => {
        const ids = GO_LIVE_STEPS.map((s) => s.id);
        const paths = GO_LIVE_STEPS.map((s) => s.path);
        expect(new Set(ids).size).toBe(ids.length);
        expect(new Set(paths).size).toBe(paths.length);
    });

    it('should give every question step questions and every data step columns', () => {
        for (const step of GO_LIVE_STEPS) {
            if (step.kind === 'questions') {
                expect(step.questions?.length ?? 0)
                    .withContext(step.id)
                    .toBeGreaterThan(0);
            }
            if (step.kind === 'data') {
                expect(step.columns?.length ?? 0)
                    .withContext(step.id)
                    .toBeGreaterThan(0);
            }
        }
    });

    it('should walk forwards and backwards through the steps', () => {
        const first = GO_LIVE_STEPS[0];
        const second = GO_LIVE_STEPS[1];
        expect(previousStep(first.path)).toBeUndefined();
        expect(nextStep(first.path)?.id).toBe(second.id);
        expect(previousStep(second.path)?.id).toBe(first.id);
        expect(nextStep(GO_LIVE_STEPS[GO_LIVE_STEPS.length - 1].path)).toBeUndefined();
    });
});

describe('GoLiveStateService', () => {
    let state: GoLiveStateService;

    /** Cross-check by id, throwing rather than asserting non-null, which eslint forbids. */
    function check(id: string) {
        const found = state.crossChecks().find((c) => c.id === id);
        if (!found) {
            throw new Error('no cross-check called ' + id);
        }
        return found;
    }

    function step(id: string) {
        const found = findStepById(id);
        if (!found) {
            throw new Error('no step called ' + id);
        }
        return found;
    }

    /** Employee rows carrying just the fields the cross-checks read. */
    function employees(...codes: string[]): PasteGridRow[] {
        return codes.map((code) => ({ code, nameEn: `Name ${code}` }));
    }

    function answerEveryQuestionIn(stepId: string): void {
        const step = findStepById(stepId);
        for (const q of step?.questions ?? []) {
            if (q.phase === 'later') {
                continue;
            }
            if (q.showWhen && !q.showWhen(state.answers())) {
                continue;
            }
            const value = q.options?.[0]?.value ?? (q.control === 'number' ? 1 : 'answer');
            state.setAnswer({ ref: q.ref, value, note: '' });
        }
    }

    beforeEach(() => {
        TestBed.configureTestingModule({});
        state = TestBed.inject(GoLiveStateService);
        state.reset();
    });

    it('should start with every step not started', () => {
        for (const step of GO_LIVE_STEPS) {
            expect(state.statusOf(step.id)).withContext(step.id).toBe('not-started');
        }
    });

    it('should not be ready for first payroll before anything is entered', () => {
        expect(state.readyForFirstPayroll()).toBe(false);
    });

    it('should move a question step to in-progress then ready', () => {
        state.setAnswer({ ref: 'Q1.01', value: 'Spa 180', note: '' });
        expect(state.statusOf('S1.1')).toBe('in-progress');

        answerEveryQuestionIn('S1.1');
        expect(state.statusOf('S1.1')).toBe('ready');
    });

    it('should ignore questions badged later when deciding a step is ready', () => {
        answerEveryQuestionIn('S1.2');
        // Q2.10 is phase 'later' and deliberately left unanswered.
        expect(state.answerFor('Q2.10')).toBeNull();
        expect(state.statusOf('S1.2')).toBe('ready');
    });

    it('should not require a hidden question before a step counts as ready', () => {
        // Q2.08 only shows when salaries are not cash-only.
        answerEveryQuestionIn('S1.2');
        state.setAnswer({ ref: 'Q2.06', value: 'CashOnly', note: '' });

        expect(state.statusOf('S1.2')).toBe('ready');
    });

    it('should mark a data step ready once rows are in and nothing is flagged', () => {
        state.setRows('S3.1', employees('1001', '1002'));
        expect(state.statusOf('S3.1')).toBe('ready');
    });

    it('should mark a data step as needing attention while it has issues', () => {
        state.setRows('S3.1', employees('1001'));
        state.setIssues('S3.1', [{ row: 0, key: 'civilId', header: 'Civil ID', issue: { key: 'Bad' } }]);

        expect(state.statusOf('S3.1')).toBe('attention');
    });

    it('should report the headcount check as waiting until Q1.05 is answered', () => {
        const result = check('headcount');
        expect(result.notStarted).toBe(true);
        expect(result.ok).toBe(true);
    });

    it('should fail the headcount check when the employee count disagrees with Q1.05', () => {
        state.setAnswer({ ref: 'Q1.05', value: 3, note: '' });
        state.setRows('S3.1', employees('1001', '1002'));

        const result = check('headcount');
        expect(result.ok).toBe(false);
        expect(result.params).toEqual([2, 3]);
    });

    it('should pass the headcount check when they agree', () => {
        state.setAnswer({ ref: 'Q1.05', value: 2, note: '' });
        state.setRows('S3.1', employees('1001', '1002'));

        expect(check('headcount').ok).toBe(true);
    });

    it('should detect duplicate employee codes across the sheet', () => {
        state.setRows('S3.1', employees('1001', '1002', '1001'));

        const result = check('duplicates');
        expect(result.ok).toBe(false);
        expect(result.params).toEqual([1]);
    });

    it('should count employees missing a salary row', () => {
        state.setRows('S3.1', employees('1001', '1002', '1003'));
        state.setRows('S3.2', [{ code: '1001', basic: 650 }]);

        const result = check('salaries');
        expect(result.ok).toBe(false);
        expect(result.params).toEqual([2]);
    });

    it('should pass the salary check once every employee is covered', () => {
        state.setRows('S3.1', employees('1001', '1002'));
        state.setRows('S3.2', [
            { code: '1001', basic: 650 },
            { code: '1002', basic: 800 },
        ]);

        expect(check('salaries').ok).toBe(true);
    });

    it('should ignore blank padding rows when matching employees to salaries', () => {
        state.setRows('S3.1', [...employees('1001'), { code: null, nameEn: null }]);
        state.setRows('S3.2', [{ code: '1001', basic: 650 }, { code: null }]);

        expect(check('salaries').ok).toBe(true);
    });

    it('should keep the readiness gate closed while a consistency check fails', () => {
        for (const step of GO_LIVE_STEPS) {
            if (step.kind === 'questions') {
                answerEveryQuestionIn(step.id);
            }
        }
        state.setRows('S1.3', [{ name: 'Basic Salary', kind: 'Earning' }]);
        state.setRows('S3.1', employees('1001'));
        state.setRows('S3.2', [{ code: '1001' }]);
        state.setRows('S3.3', [{ code: '1001' }]);
        state.setRows('S3.4', [{ code: '1001' }]);
        // Q1.05 was auto-answered as 1 by answerEveryQuestionIn, so the headcount check passes.

        expect(state.blockingChecks().length).toBe(0);
        expect(state.readyForFirstPayroll()).toBe(true);

        // Break one check and the gate must close again.
        state.setRows('S3.3', []);
        expect(state.readyForFirstPayroll()).toBe(false);
    });

    it('should exclude the placeholder trial-payroll step from the gate', () => {
        expect(state.gatingSteps().some((s) => s.id === 'S4.2')).toBe(false);
    });

    it('should report question progress as answered of total', () => {
        state.setAnswer({ ref: 'Q1.01', value: 'Spa 180', note: '' });
        const progress = state.progressOf(step('S1.1'));

        expect(progress.done).toBe(1);
        expect(progress.total).toBeGreaterThan(1);
    });

    it('should keep notes alongside answers', () => {
        state.setAnswer({ ref: 'Q2.04', value: 'Fixed30', note: 'differs for site staff' });

        expect(state.answerFor('Q2.04')).toBe('Fixed30');
        expect(state.noteFor('Q2.04')).toBe('differs for site staff');
    });
});
