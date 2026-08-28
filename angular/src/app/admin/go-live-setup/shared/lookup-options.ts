import { PasteGridColumn } from './paste-grid/paste-grid.types';
import { GoLiveQuestion, QuestionOption } from './question-card/go-live-question.types';

/**
 * Turns a registry declaration of `optionsFrom: 'Nationality'` into the values the tenant actually has.
 *
 * Kept as pure functions rather than a method on the state service so the substitution rule is testable
 * on its own, and so the grid and the question card cannot drift apart in how they resolve a lookup.
 */

/** Lookup name to its values, as returned by the backend. */
export type LookupOptionMap = Record<string, string[]>;

/**
 * A column with its options filled in. Returns the original object untouched when there is nothing to
 * substitute, so change detection does not see a new reference on every render.
 */
export function resolveColumnOptions(column: PasteGridColumn, lookups: LookupOptionMap): PasteGridColumn {
    if (!column.optionsFrom) {
        return column;
    }

    const values = lookups[column.optionsFrom];
    if (!values || values.length === 0) {
        // The tenant has not populated this lookup yet. Free text beats an empty dropdown the customer
        // cannot escape from -- validation will still flag the value once the list exists.
        return { ...column, type: 'text', options: undefined };
    }

    return { ...column, type: 'choice', options: values };
}

export function resolveColumns(columns: PasteGridColumn[], lookups: LookupOptionMap): PasteGridColumn[] {
    return columns.map((c) => resolveColumnOptions(c, lookups));
}

/** Same rule for a question. An unpopulated lookup falls back to free text for the same reason. */
export function resolveQuestionOptions(question: GoLiveQuestion, lookups: LookupOptionMap): GoLiveQuestion {
    if (!question.optionsFrom || question.options) {
        return question;
    }

    const values = lookups[question.optionsFrom];
    if (!values || values.length === 0) {
        return { ...question, control: 'text' };
    }

    const options: QuestionOption[] = values.map((v) => ({ value: v, label: v }));
    return { ...question, control: 'choice', options };
}

export function resolveQuestions(questions: GoLiveQuestion[], lookups: LookupOptionMap): GoLiveQuestion[] {
    return questions.map((q) => resolveQuestionOptions(q, lookups));
}
