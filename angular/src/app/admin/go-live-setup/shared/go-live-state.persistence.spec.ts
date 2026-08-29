import { TestBed } from '@angular/core/testing';
import { GoLiveStateService } from './go-live-state.service';
import { findStepByQuestionRef, findStepById } from './go-live-registry';

/**
 * The persistence-facing half of the state service: what gets marked dirty, what a step serialises to,
 * and that hydrating a saved draft does not look like the customer editing it.
 */
describe('GoLiveStateService persistence', () => {
    let state: GoLiveStateService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        state = TestBed.inject(GoLiveStateService);
        state.reset();
    });

    // ---------------------------------------------------------------- attribution

    it('should attribute a question to its owning step', () => {
        expect(findStepByQuestionRef('Q2.04')?.id).toBe('S1.2');
        expect(findStepByQuestionRef('Q5.01')?.id).toBe('S1.5');
        expect(findStepByQuestionRef('nonsense')).toBeUndefined();
    });

    it('should mark the owning step dirty when an answer changes', () => {
        state.setAnswer({ ref: 'Q2.04', value: 'Fixed30', note: '' });

        expect([...state.dirtySteps()]).toEqual(['S1.2']);
    });

    it('should mark a data step dirty when its rows change', () => {
        state.setRows('S3.1', [{ code: '1001' }]);

        expect([...state.dirtySteps()]).toEqual(['S3.1']);
    });

    it('should accumulate dirty steps rather than replacing them', () => {
        state.setAnswer({ ref: 'Q2.04', value: 'Fixed30', note: '' });
        state.setAnswer({ ref: 'Q5.01', value: 30, note: '' });
        state.setRows('S3.1', [{ code: '1001' }]);

        expect([...state.dirtySteps()].sort()).toEqual(['S1.2', 'S1.5', 'S3.1']);
    });

    it('should clear only the steps it is told to', () => {
        state.setAnswer({ ref: 'Q2.04', value: 'Fixed30', note: '' });
        state.setRows('S3.1', [{ code: '1001' }]);

        state.clearDirty(['S1.2']);

        expect([...state.dirtySteps()]).toEqual(['S3.1']);
    });

    // ---------------------------------------------------------------- payloads

    it('should serialise a question step as answers and notes', () => {
        state.setAnswer({ ref: 'Q2.04', value: 'Fixed30', note: 'differs for site staff' });

        const payload = JSON.parse(state.payloadFor('S1.2') ?? '{}');

        expect(payload.answers['Q2.04']).toBe('Fixed30');
        expect(payload.notes['Q2.04']).toBe('differs for site staff');
    });

    it('should not include questions from another step in a step payload', () => {
        state.setAnswer({ ref: 'Q2.04', value: 'Fixed30', note: '' });
        state.setAnswer({ ref: 'Q5.01', value: 30, note: '' });

        const payload = JSON.parse(state.payloadFor('S1.2') ?? '{}');

        expect(Object.keys(payload.answers)).toEqual(['Q2.04']);
    });

    it('should serialise a data step as rows, dropping blank padding', () => {
        state.setRows('S3.1', [{ code: '1001' }, { code: null, nameEn: null }]);

        const payload = JSON.parse(state.payloadFor('S3.1') ?? '{}');

        expect(payload.rows.length).toBe(1);
        expect(payload.rows[0].code).toBe('1001');
    });

    it('should return null for a step holding nothing, so the row is deleted rather than stored empty', () => {
        expect(state.payloadFor('S1.2')).toBeNull();
        expect(state.payloadFor('S3.1')).toBeNull();
    });

    it('should return null for a step that does not exist', () => {
        expect(state.payloadFor('S9.9')).toBeNull();
    });

    it('should keep a note even when the answer itself was cleared', () => {
        state.setAnswer({ ref: 'Q2.04', value: null, note: 'still deciding' });

        const payload = JSON.parse(state.payloadFor('S1.2') ?? '{}');

        expect(payload.answers['Q2.04']).toBeUndefined();
        expect(payload.notes['Q2.04']).toBe('still deciding');
    });

    // ---------------------------------------------------------------- hydration

    it('should restore answers, notes and rows from saved payloads', () => {
        state.hydrate([
            { stepId: 'S1.2', payload: JSON.stringify({ answers: { 'Q2.04': 'Fixed30' }, notes: { 'Q2.04': 'x' } }) },
            { stepId: 'S3.1', payload: JSON.stringify({ rows: [{ code: '1001' }] }) },
        ]);

        expect(state.answerFor('Q2.04')).toBe('Fixed30');
        expect(state.noteFor('Q2.04')).toBe('x');
        expect(state.rowsFor('S3.1')[0]['code']).toBe('1001');
    });

    it('should not mark anything dirty while hydrating', () => {
        state.hydrate([
            { stepId: 'S1.2', payload: JSON.stringify({ answers: { 'Q2.04': 'Fixed30' } }) },
            { stepId: 'S3.1', payload: JSON.stringify({ rows: [{ code: '1001' }] }) },
        ]);

        expect(state.dirtySteps().size).toBe(0);
    });

    it('should survive a payload it cannot parse rather than refusing to open', () => {
        state.hydrate([
            { stepId: 'S1.2', payload: 'not json at all' },
            { stepId: 'S1.5', payload: JSON.stringify({ answers: { 'Q5.01': 30 } }) },
        ]);

        expect(state.answerFor('Q2.04')).toBeNull();
        expect(state.answerFor('Q5.01')).toBe(30);
    });

    it('should treat an empty payload as nothing saved', () => {
        state.hydrate([{ stepId: 'S1.2', payload: '' }]);

        expect(state.answerFor('Q2.04')).toBeNull();
    });

    it('should discard whatever was there before hydrating', () => {
        state.setAnswer({ ref: 'Q2.04', value: 'ActualDays', note: '' });

        state.hydrate([{ stepId: 'S1.5', payload: JSON.stringify({ answers: { 'Q5.01': 30 } }) }]);

        expect(state.answerFor('Q2.04')).toBeNull();
        expect(state.answerFor('Q5.01')).toBe(30);
    });

    it('should round-trip a step through payloadFor and hydrate', () => {
        state.setAnswer({ ref: 'Q2.04', value: 'Fixed30', note: 'caveat' });
        state.setAnswer({ ref: 'Q2.05', value: '3', note: '' });
        const payload = state.payloadFor('S1.2');

        state.reset();
        state.hydrate([{ stepId: 'S1.2', payload: payload ?? '' }]);

        expect(state.answerFor('Q2.04')).toBe('Fixed30');
        expect(state.answerFor('Q2.05')).toBe('3');
        expect(state.noteFor('Q2.04')).toBe('caveat');
    });

    it('should restore step status from a hydrated draft', () => {
        const step = findStepById('S3.1');
        expect(step).toBeDefined();

        state.hydrate([{ stepId: 'S3.1', payload: JSON.stringify({ rows: [{ code: '1001' }] }) }]);

        expect(state.statusOf('S3.1')).toBe('ready');
    });

    it('should clear dirty steps on reset', () => {
        state.setAnswer({ ref: 'Q2.04', value: 'Fixed30', note: '' });
        state.reset();

        expect(state.dirtySteps().size).toBe(0);
    });
});
