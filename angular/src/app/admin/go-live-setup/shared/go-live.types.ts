import { PasteGridColumn } from './paste-grid/paste-grid.types';
import { GoLiveQuestion, QuestionPhase } from './question-card/go-live-question.types';

/**
 * Shape of the Go-Live Setup itself: stages, steps, and the status each step reports to the
 * readiness board.
 *
 * The registry (go-live-registry.ts) is the single place a step is declared. Routes, the navigation
 * rail and the board are all derived from it, so adding a step means editing one array.
 */

export type GoLiveStepKind =
    /** A sheet of questions rendered through question-card. */
    | 'questions'
    /** A bulk sheet rendered through paste-grid. */
    | 'data'
    /** The automated cross-checks (S4.1). */
    | 'check'
    /** Declared, routable, but waiting on backend work. Excluded from the readiness gate. */
    | 'placeholder';

export type StepStatus = 'not-started' | 'in-progress' | 'attention' | 'ready';

export interface GoLiveStep {
    /** Stable id shown to the user and used in the route, e.g. 'S1.2'. */
    id: string;

    /** URL segment under /go-live-setup/step/. */
    path: string;

    /** Localisation key for the step name. */
    nameKey: string;

    /** Localisation key for the one-line intro at the top of the step. */
    introKey?: string;

    kind: GoLiveStepKind;

    /** Steps marked 'later' do not block the first payroll and are excluded from the gate. */
    phase: QuestionPhase;

    /** Roughly how long this step takes, shown on the board. Localisation key. */
    effortKey?: string;

    questions?: GoLiveQuestion[];

    columns?: PasteGridColumn[];

    /** For 'placeholder' steps: localisation key explaining what is still missing. */
    pendingKey?: string;
}

export interface GoLiveStage {
    /** 'S1' */
    id: string;
    nameKey: string;
    steps: GoLiveStep[];
}

/** One automated consistency check on the board and on step S4.1. */
export interface CrossCheck {
    id: string;
    /** Localisation key for the check's name. */
    labelKey: string;
    ok: boolean;
    /** Localisation key for the result line, given `params`. */
    detailKey: string;
    params: (string | number)[];
    /** Step to jump to when it fails. */
    stepId?: string;
    /** True while the check has nothing to look at yet, so the board can say "waiting" not "failing". */
    notStarted: boolean;
}
