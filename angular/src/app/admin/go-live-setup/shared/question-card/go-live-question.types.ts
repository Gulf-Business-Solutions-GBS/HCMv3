/** Contracts for the Go-Live Setup's question sheets (steps S1.1 - S1.7, S4, S6).
 *
 *  A question is DATA, not UI. In production these descriptors come from the server-side go-live
 *  template definition -- the same definition that generates the offline workbook -- already localised.
 *  Only the card's own chrome (badges, "add a note", placeholders) is localised in the client.
 */

export type AnswerValue = string | number | null;

/** Every answer given so far, keyed by question ref. Passed to showWhen/preview so a question can
 *  react to earlier answers -- the thing a spreadsheet fundamentally cannot do. */
export type AnswerMap = Record<string, AnswerValue>;

export type QuestionControl = 'text' | 'longtext' | 'number' | 'date' | 'choice' | 'yesno';

/** Whether an answer is needed before the first payroll can run, or can wait. Drives the badge and the
 *  readiness board's "ready to run first payroll" gate. */
export type QuestionPhase = 'first-payroll' | 'later';

export interface QuestionOption {
    value: string;
    label: string;
    /** One line under the option. Use it to say what the option means in practice, not to repeat it. */
    hint?: string;
}

export interface GoLiveQuestion {
    /** Stable id, e.g. 'Q2.04'. Survives rewording, and is what the configurator maps to a setting. */
    ref: string;

    question: string;

    /** Shown under the question. The place for "pick the rule your finance team uses today". */
    help?: string;

    control?: QuestionControl;

    options?: QuestionOption[];

    /** Greyed placeholder for free-text and number answers. */
    example?: string;

    /** Appended after a number input: 'days', 'KD'. */
    unit?: string;

    phase?: QuestionPhase;

    required?: boolean;

    min?: number;
    max?: number;

    /** Set false to hide the note box on a question where a caveat makes no sense. */
    allowNote?: boolean;

    /**
     * Render only when this returns true. Answering "we don't track daily attendance" should remove the
     * twelve attendance questions outright rather than leave them looking unanswered.
     */
    showWhen?: (answers: AnswerMap) => boolean;

    /**
     * A worked example recomputed as the answer changes, so an abstract rule becomes a number the
     * customer can recognise as right or wrong. This is the single highest-value thing Go-Live Setup does
     * that the offline workbook could not.
     */
    preview?: (value: AnswerValue, answers: AnswerMap) => string | null;
}

/** What the card emits when the user answers or annotates. */
export interface AnswerChange {
    ref: string;
    value: AnswerValue;
    note: string;
}

export function isAnswered(value: AnswerValue): boolean {
    return value !== null && value !== undefined && value !== '';
}

/** Questions the customer can still see, given what they have answered so far. */
export function visibleQuestions(questions: GoLiveQuestion[], answers: AnswerMap): GoLiveQuestion[] {
    return questions.filter((q) => !q.showWhen || q.showWhen(answers));
}

/** Progress for the readiness board: only first-payroll questions count toward the gate. */
export function countOutstanding(questions: GoLiveQuestion[], answers: AnswerMap): number {
    return visibleQuestions(questions, answers).filter(
        (q) => q.phase !== 'later' && !isAnswered(answers[q.ref] ?? null)
    ).length;
}
