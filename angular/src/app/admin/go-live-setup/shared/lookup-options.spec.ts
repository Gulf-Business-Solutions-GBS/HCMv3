import { resolveColumnOptions, resolveColumns, resolveQuestionOptions, resolveQuestions } from './lookup-options';
import { PasteGridColumn } from './paste-grid/paste-grid.types';
import { GoLiveQuestion } from './question-card/go-live-question.types';
import { GO_LIVE_STEPS } from './go-live-registry';

describe('resolveColumnOptions', () => {
    const lookups = {
        Nationality: ['Egyptian', 'Kuwaiti'],
        Bank: [],
    };

    it('should leave a column without optionsFrom untouched, by reference', () => {
        const column: PasteGridColumn = { key: 'a', header: 'A' };
        expect(resolveColumnOptions(column, lookups)).toBe(column);
    });

    it('should fill options from the named lookup', () => {
        const column: PasteGridColumn = { key: 'n', header: 'Nationality', optionsFrom: 'Nationality' };
        const resolved = resolveColumnOptions(column, lookups);

        expect(resolved.type).toBe('choice');
        expect(resolved.options).toEqual(['Egyptian', 'Kuwaiti']);
    });

    it('should fall back to free text when the tenant has not populated the lookup', () => {
        const column: PasteGridColumn = { key: 'b', header: 'Bank', optionsFrom: 'Bank' };
        const resolved = resolveColumnOptions(column, lookups);

        expect(resolved.type).toBe('text');
        expect(resolved.options).toBeUndefined();
    });

    it('should fall back to free text when the lookup is entirely absent', () => {
        const column: PasteGridColumn = { key: 'x', header: 'X', optionsFrom: 'DoesNotExist' };
        expect(resolveColumnOptions(column, lookups).type).toBe('text');
    });

    it('should not mutate the registry column it was given', () => {
        const column: PasteGridColumn = { key: 'n', header: 'Nationality', optionsFrom: 'Nationality' };
        resolveColumnOptions(column, lookups);

        expect(column.options).toBeUndefined();
        expect(column.type).toBeUndefined();
    });

    it('should keep the rest of the column definition', () => {
        const column: PasteGridColumn = {
            key: 'n',
            header: 'Nationality',
            width: 130,
            required: true,
            hint: 'pick one',
            optionsFrom: 'Nationality',
        };
        const resolved = resolveColumnOptions(column, lookups);

        expect(resolved.width).toBe(130);
        expect(resolved.required).toBe(true);
        expect(resolved.hint).toBe('pick one');
    });

    it('should resolve a whole column set', () => {
        const columns: PasteGridColumn[] = [
            { key: 'a', header: 'A' },
            { key: 'n', header: 'N', optionsFrom: 'Nationality' },
        ];
        const resolved = resolveColumns(columns, lookups);

        expect(resolved[0]).toBe(columns[0]);
        expect(resolved[1].options?.length).toBe(2);
    });
});

describe('resolveQuestionOptions', () => {
    const lookups = { Currency: ['Kuwaiti Dinar', 'US Dollar'] };

    it('should build choice options from the named lookup', () => {
        const question: GoLiveQuestion = { ref: 'Q1.04', question: 'Currency?', optionsFrom: 'Currency' };
        const resolved = resolveQuestionOptions(question, lookups);

        expect(resolved.control).toBe('choice');
        expect(resolved.options).toEqual([
            { value: 'Kuwaiti Dinar', label: 'Kuwaiti Dinar' },
            { value: 'US Dollar', label: 'US Dollar' },
        ]);
    });

    it('should leave an explicit option list alone', () => {
        const question: GoLiveQuestion = {
            ref: 'Q1',
            question: 'Pick',
            optionsFrom: 'Currency',
            options: [{ value: 'a', label: 'A' }],
        };
        expect(resolveQuestionOptions(question, lookups)).toBe(question);
    });

    it('should fall back to free text when the lookup is empty', () => {
        const question: GoLiveQuestion = { ref: 'Q1', question: 'Bank?', optionsFrom: 'Bank' };
        expect(resolveQuestionOptions(question, lookups).control).toBe('text');
    });

    it('should resolve a whole question set', () => {
        const questions: GoLiveQuestion[] = [
            { ref: 'A', question: 'a' },
            { ref: 'B', question: 'b', optionsFrom: 'Currency' },
        ];
        const resolved = resolveQuestions(questions, lookups);

        expect(resolved[0]).toBe(questions[0]);
        expect(resolved[1].options?.length).toBe(2);
    });
});

describe('registry lookup wiring', () => {
    /**
     * Guards the swap that replaced the hard-coded arrays: a column may declare a static option list or
     * a lookup, never both, and a choice column must have one of them or it silently becomes free text.
     */
    it('should not declare both options and optionsFrom on any column', () => {
        for (const step of GO_LIVE_STEPS) {
            for (const column of step.columns ?? []) {
                const both = !!column.options && !!column.optionsFrom;
                expect(both).withContext(`${step.id}/${column.key}`).toBe(false);
            }
        }
    });

    it('should give every choice column a source of options', () => {
        for (const step of GO_LIVE_STEPS) {
            for (const column of step.columns ?? []) {
                if (column.type === 'choice') {
                    const hasSource = (column.options?.length ?? 0) > 0 || !!column.optionsFrom;
                    expect(hasSource).withContext(`${step.id}/${column.key}`).toBe(true);
                }
            }
        }
    });

    it('should give every choice question a source of options', () => {
        for (const step of GO_LIVE_STEPS) {
            for (const question of step.questions ?? []) {
                if (question.control === 'choice') {
                    const hasSource = (question.options?.length ?? 0) > 0 || !!question.optionsFrom;
                    expect(hasSource).withContext(`${step.id}/${question.ref}`).toBe(true);
                }
            }
        }
    });

    /** The five the loader fetches. A sixth added to the registry needs adding there too. */
    it('should only reference lookups the loader actually fetches', () => {
        const loaded = ['Nationality', 'Bank', 'JobTitle', 'WorkLocation', 'Currency'];

        for (const step of GO_LIVE_STEPS) {
            for (const column of step.columns ?? []) {
                if (column.optionsFrom) {
                    expect(loaded).withContext(`${step.id}/${column.key}`).toContain(column.optionsFrom);
                }
            }
            for (const question of step.questions ?? []) {
                if (question.optionsFrom) {
                    expect(loaded).withContext(`${step.id}/${question.ref}`).toContain(question.optionsFrom);
                }
            }
        }
    });
});
