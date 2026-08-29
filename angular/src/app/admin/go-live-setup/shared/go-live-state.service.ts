import { Injectable, computed, signal } from '@angular/core';
import { PasteGridIssue, PasteGridRow, isRowEmpty } from './paste-grid/paste-grid.types';
import { AnswerChange, AnswerMap, isAnswered, visibleQuestions } from './question-card/go-live-question.types';
import { GO_LIVE_STAGES, GO_LIVE_STEPS, findStepByQuestionRef } from './go-live-registry';
import { GoLiveStep, CrossCheck, StepStatus } from './go-live.types';
import { LookupOptionMap } from './lookup-options';

/**
 * Everything the customer has entered, plus everything the readiness board derives from it.
 *
 * Held in memory for now. When the backend lands this becomes the only file that changes: the signals
 * get hydrated from the server and each setter also queues an autosave. Nothing that reads this service
 * needs to know which of those is happening.
 *
 * Provided in root rather than on the shell so answers survive navigating away from Go-Live Setup and
 * back, which is what a customer filling this over several days will do.
 */
@Injectable({ providedIn: 'root' })
export class GoLiveStateService {
    private readonly answersSignal = signal<AnswerMap>({});
    private readonly notesSignal = signal<Record<string, string>>({});
    private readonly rowsSignal = signal<Record<string, PasteGridRow[]>>({});
    private readonly issuesSignal = signal<Record<string, PasteGridIssue[]>>({});

    /** Dropdown values for every choice column and question, keyed by lookup name. */
    private readonly lookupsSignal = signal<LookupOptionMap>({});
    private readonly lookupsLoadedSignal = signal(false);

    /** Steps changed since the last successful save. The persistence service drains this. */
    private readonly dirtyStepsSignal = signal<ReadonlySet<string>>(new Set());

    /** True while hydrating, so restoring a draft does not look like the customer editing it. */
    private hydrating = false;

    readonly answers = this.answersSignal.asReadonly();
    readonly notes = this.notesSignal.asReadonly();
    readonly lookups = this.lookupsSignal.asReadonly();

    /** False until the first load returns, so a step can avoid rendering an empty dropdown. */
    readonly lookupsLoaded = this.lookupsLoadedSignal.asReadonly();
    readonly dirtySteps = this.dirtyStepsSignal.asReadonly();

    /** Status of every step, keyed by step id. Recomputed whenever anything changes. */
    readonly statuses = computed<Record<string, StepStatus>>(() => {
        const out: Record<string, StepStatus> = {};
        for (const step of GO_LIVE_STEPS) {
            out[step.id] = this.computeStatus(step);
        }
        return out;
    });

    readonly crossChecks = computed<CrossCheck[]>(() => this.computeCrossChecks());

    /** Steps that must be ready before a trial payroll can run. Placeholders are excluded. */
    readonly gatingSteps = computed(() =>
        GO_LIVE_STEPS.filter((s) => s.phase !== 'later' && s.kind !== 'placeholder' && s.kind !== 'check')
    );

    readonly readyStepCount = computed(() => {
        const statuses = this.statuses();
        return this.gatingSteps().filter((s) => statuses[s.id] === 'ready').length;
    });

    readonly totalGatingSteps = computed(() => this.gatingSteps().length);

    readonly blockingChecks = computed(() => this.crossChecks().filter((c) => !c.ok));

    readonly readyForFirstPayroll = computed(
        () => this.readyStepCount() === this.totalGatingSteps() && this.blockingChecks().length === 0
    );

    readonly stages = GO_LIVE_STAGES;

    answerFor(ref: string) {
        return this.answersSignal()[ref] ?? null;
    }

    noteFor(ref: string): string {
        return this.notesSignal()[ref] ?? '';
    }

    rowsFor(stepId: string): PasteGridRow[] {
        return this.rowsSignal()[stepId] ?? [];
    }

    issuesFor(stepId: string): PasteGridIssue[] {
        return this.issuesSignal()[stepId] ?? [];
    }

    filledRowCount(stepId: string): number {
        return this.rowsFor(stepId).filter((r) => !isRowEmpty(r)).length;
    }

    setAnswer(change: AnswerChange): void {
        this.answersSignal.update((a) => ({ ...a, [change.ref]: change.value }));
        this.notesSignal.update((n) => ({ ...n, [change.ref]: change.note }));
        this.markDirty(findStepByQuestionRef(change.ref)?.id);
    }

    setRows(stepId: string, rows: PasteGridRow[]): void {
        this.rowsSignal.update((r) => ({ ...r, [stepId]: rows }));
        this.markDirty(stepId);
    }

    setIssues(stepId: string, issues: PasteGridIssue[]): void {
        this.issuesSignal.update((i) => ({ ...i, [stepId]: issues }));
    }

    /** Replaces the whole map. Called once when the shell loads, and again after a reference
     *  pack is seeded or the customer adds a job title. */
    setLookups(lookups: LookupOptionMap): void {
        this.lookupsSignal.set(lookups);
        this.lookupsLoadedSignal.set(true);
    }

    statusOf(stepId: string): StepStatus {
        return this.statuses()[stepId] ?? 'not-started';
    }

    /** Answered / total for a question step, for the board's count column. */
    progressOf(step: GoLiveStep): { done: number; total: number } {
        if (step.kind === 'questions') {
            const required = visibleQuestions(step.questions ?? [], this.answersSignal()).filter(
                (q) => q.phase !== 'later'
            );
            const done = required.filter((q) => isAnswered(this.answerFor(q.ref))).length;
            return { done, total: required.length };
        }
        if (step.kind === 'data') {
            const filled = this.filledRowCount(step.id);
            return { done: filled, total: filled };
        }
        return { done: 0, total: 0 };
    }

    /**
     * Restores a saved draft. Runs with dirty tracking suppressed so hydrating does not
     * immediately queue a save of everything it just loaded.
     */
    hydrate(steps: { stepId: string; payload: string }[]): void {
        this.hydrating = true;
        try {
            const answers: AnswerMap = {};
            const notes: Record<string, string> = {};
            const rows: Record<string, PasteGridRow[]> = {};

            for (const step of steps) {
                const parsed = this.parse(step.payload);
                if (!parsed) {
                    continue;
                }
                Object.assign(answers, parsed.answers ?? {});
                Object.assign(notes, parsed.notes ?? {});
                if (parsed.rows) {
                    rows[step.stepId] = parsed.rows;
                }
            }

            this.answersSignal.set(answers);
            this.notesSignal.set(notes);
            this.rowsSignal.set(rows);
            this.dirtyStepsSignal.set(new Set());
        } finally {
            this.hydrating = false;
        }
    }

    /** The JSON a step should be saved as, or null when the step holds nothing worth saving. */
    payloadFor(stepId: string): string | null {
        const step = GO_LIVE_STEPS.find((s) => s.id === stepId);
        if (!step) {
            return null;
        }

        if (step.kind === 'data') {
            const rows = this.rowsFor(stepId).filter((r) => !isRowEmpty(r));
            return rows.length ? JSON.stringify({ rows }) : null;
        }

        const answers: AnswerMap = {};
        const notes: Record<string, string> = {};
        for (const question of step.questions ?? []) {
            const value = this.answerFor(question.ref);
            if (value !== null && value !== '') {
                answers[question.ref] = value;
            }
            const note = this.noteFor(question.ref);
            if (note) {
                notes[question.ref] = note;
            }
        }

        return Object.keys(answers).length || Object.keys(notes).length ? JSON.stringify({ answers, notes }) : null;
    }

    clearDirty(stepIds: string[]): void {
        this.dirtyStepsSignal.update((current) => {
            const next = new Set(current);
            stepIds.forEach((id) => next.delete(id));
            return next;
        });
    }

    reset(): void {
        this.answersSignal.set({});
        this.notesSignal.set({});
        this.rowsSignal.set({});
        this.issuesSignal.set({});
        this.dirtyStepsSignal.set(new Set());
    }

    private markDirty(stepId: string | undefined): void {
        if (!stepId || this.hydrating) {
            return;
        }
        this.dirtyStepsSignal.update((current) => new Set(current).add(stepId));
    }

    private parse(
        payload: string
    ): { answers?: AnswerMap; notes?: Record<string, string>; rows?: PasteGridRow[] } | null {
        try {
            return payload ? JSON.parse(payload) : null;
        } catch {
            // A payload we cannot read is a draft we cannot restore. Dropping it beats refusing
            // to open the screen at all; the customer re-enters that step.
            return null;
        }
    }

    private computeStatus(step: GoLiveStep): StepStatus {
        switch (step.kind) {
            case 'questions': {
                const required = visibleQuestions(step.questions ?? [], this.answersSignal()).filter(
                    (q) => q.phase !== 'later'
                );
                const done = required.filter((q) => isAnswered(this.answerFor(q.ref))).length;
                if (done === 0) {
                    return 'not-started';
                }
                return done === required.length ? 'ready' : 'in-progress';
            }

            case 'data': {
                if (this.filledRowCount(step.id) === 0) {
                    return 'not-started';
                }
                return this.issuesFor(step.id).length > 0 ? 'attention' : 'ready';
            }

            case 'check': {
                const checks = this.crossChecks();
                if (checks.every((c) => c.notStarted)) {
                    return 'not-started';
                }
                return checks.some((c) => !c.ok) ? 'attention' : 'ready';
            }

            default:
                return 'not-started';
        }
    }

    /**
     * The three consistency failures that actually stop a payroll, plus duplicate codes.
     *
     * Duplicate detection lives here rather than as a paste-grid column rule because a duplicate is a
     * property of the whole sheet, not of one cell.
     */
    private computeCrossChecks(): CrossCheck[] {
        const employees = this.rowsFor('S3.1').filter((r) => !isRowEmpty(r));
        const employeeCodes = employees.map((r) => String(r['code'] ?? '').trim()).filter((c) => c !== '');
        const codeSet = new Set(employeeCodes);

        const declared = this.answerFor('Q1.05');
        const declaredCount = typeof declared === 'number' ? declared : null;

        const duplicates = employeeCodes.length - codeSet.size;

        const missingFrom = (stepId: string): number => {
            const present = new Set(
                this.rowsFor(stepId)
                    .filter((r) => !isRowEmpty(r))
                    .map((r) => String(r['code'] ?? '').trim())
            );
            return [...codeSet].filter((c) => !present.has(c)).length;
        };

        const noEmployees = codeSet.size === 0;

        return [
            {
                id: 'headcount',
                labelKey: 'GoLiveCheckHeadcount',
                ok: declaredCount === null || declaredCount === codeSet.size,
                detailKey: declaredCount === null ? 'GoLiveCheckHeadcountUnknown' : 'GoLiveCheckHeadcountDetail',
                params: declaredCount === null ? [] : [codeSet.size, declaredCount],
                stepId: 'S3.1',
                notStarted: noEmployees && declaredCount === null,
            },
            {
                id: 'duplicates',
                labelKey: 'GoLiveCheckDuplicateCodes',
                ok: duplicates === 0,
                detailKey: duplicates === 0 ? 'GoLiveCheckNoDuplicates' : 'GoLiveCheckDuplicatesFound',
                params: [duplicates],
                stepId: 'S3.1',
                notStarted: noEmployees,
            },
            {
                id: 'salaries',
                labelKey: 'GoLiveCheckSalaryRows',
                ok: !noEmployees && missingFrom('S3.2') === 0,
                detailKey: noEmployees
                    ? 'GoLiveCheckAwaitingEmployees'
                    : missingFrom('S3.2') === 0
                      ? 'GoLiveCheckAllCovered'
                      : 'GoLiveCheckMissingRows',
                params: [missingFrom('S3.2')],
                stepId: 'S3.2',
                notStarted: noEmployees,
            },
            {
                id: 'banks',
                labelKey: 'GoLiveCheckBankRows',
                ok: !noEmployees && missingFrom('S3.3') === 0,
                detailKey: noEmployees
                    ? 'GoLiveCheckAwaitingEmployees'
                    : missingFrom('S3.3') === 0
                      ? 'GoLiveCheckAllCovered'
                      : 'GoLiveCheckMissingRows',
                params: [missingFrom('S3.3')],
                stepId: 'S3.3',
                notStarted: noEmployees,
            },
        ];
    }
}
