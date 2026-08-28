import { Component, computed, signal } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { QuestionCardComponent } from '../go-live-setup/shared/question-card/question-card.component';
import {
    AnswerChange,
    AnswerMap,
    AnswerValue,
    GoLiveQuestion,
    countOutstanding,
    visibleQuestions,
} from '../go-live-setup/shared/question-card/go-live-question.types';

/** Reference month for the proration worked example: August 2026, 31 days, weekly offs Friday+Saturday. */
const SAMPLE_YEAR = 2026;
const SAMPLE_MONTH = 7; // zero-based: August
const SAMPLE_JOIN_DAY = 16;
const SAMPLE_SALARY = 650;

/**
 * Harness for the Go-Live Setup question card, using the real S1.2 "How you pay" step.
 *
 * Shows the two things the offline workbook could not do:
 *  - Q2.04 recomputes a worked example from the selected option, so the customer sees the actual dinars
 *    a mid-month joiner would receive rather than choosing between four abstract phrasings;
 *  - Q2.08 disappears entirely when Q2.06 says salaries are paid in cash.
 *
 * Question text is passed as data, not localisation keys, because in production these descriptors come
 * from the server-side go-live template definition already localised -- the same definition that
 * generates the offline workbook. Only the card's own chrome is localised in the client.
 */
@Component({
    templateUrl: './demo-ui-question-card.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, QuestionCardComponent, LocalizePipe],
})
export class DemoUiQuestionCardComponent extends AppComponentBase {
    readonly answers = signal<AnswerMap>({});
    readonly notes = signal<Record<string, string>>({});

    readonly questions: GoLiveQuestion[] = [
        {
            ref: 'Q2.01',
            question: 'How often do you pay salaries?',
            control: 'choice',
            phase: 'first-payroll',
            options: [
                { value: 'Monthly', label: 'Once a month' },
                { value: 'SemiMonthly', label: 'Twice a month' },
                { value: 'Fortnightly', label: 'Every two weeks' },
                { value: 'Weekly', label: 'Weekly' },
            ],
        },
        {
            ref: 'Q2.02',
            question: 'On which day of the month do salaries reach employees?',
            control: 'number',
            unit: 'day of the month',
            example: '28',
            min: 1,
            max: 31,
            phase: 'first-payroll',
        },
        {
            ref: 'Q2.03',
            question: 'What is your payroll cut-off day?',
            help: 'Changes made after this day of the month go into the next payroll instead.',
            control: 'number',
            unit: 'day of the month',
            example: '25',
            min: 1,
            max: 31,
            phase: 'first-payroll',
        },
        {
            ref: 'Q2.04',
            question:
                'When someone joins, leaves or is on unpaid leave part way through a month, how do you work out their part-month pay?',
            help: 'Pick the rule your finance team uses today. If different rules apply to different staff, say so in the notes.',
            control: 'choice',
            phase: 'first-payroll',
            options: [
                { value: 'Fixed30', label: 'Always divide the monthly salary by 30 days' },
                { value: 'ActualDays', label: 'Divide by the actual number of days in that month' },
                { value: 'WorkingDays', label: 'Divide by the number of working days in that month' },
                { value: 'NoProration', label: 'Never prorate - they get the full monthly amount' },
            ],
            preview: (value) => this.prorationExample(value),
        },
        {
            ref: 'Q2.05',
            question: 'To how many decimal places should salary amounts be rounded?',
            control: 'choice',
            phase: 'first-payroll',
            options: [
                { value: '2', label: '2 decimal places' },
                { value: '3', label: '3 decimal places', hint: 'Kuwaiti Dinar is normally 3.' },
            ],
        },
        {
            ref: 'Q2.06',
            question: 'How are salaries paid?',
            control: 'choice',
            phase: 'first-payroll',
            options: [
                { value: 'BankOnly', label: 'Bank transfer only' },
                { value: 'Mixed', label: 'Mostly bank transfer, some cash' },
                { value: 'CashOnly', label: 'Cash only' },
            ],
        },
        {
            ref: 'Q2.08',
            question: 'Does that bank need the transfer file in a particular format?',
            help: 'If yes, please send us one real file the bank has already accepted.',
            control: 'choice',
            phase: 'first-payroll',
            options: [
                { value: 'Sample', label: 'Yes - we will attach a sample file' },
                { value: 'Standard', label: 'No - a standard file is fine' },
                { value: 'Unsure', label: 'Not sure - please advise' },
            ],
            // Nobody paid only in cash should ever see a bank-file question.
            showWhen: (answers) => answers['Q2.06'] !== 'CashOnly',
        },
        {
            ref: 'Q2.10',
            question: 'Do you need payroll costs posted into your accounting system?',
            help: 'If yes we will need your chart of accounts and a sample journal.',
            control: 'choice',
            phase: 'later',
            options: [
                { value: 'AtGoLive', label: 'Yes - at go-live' },
                { value: 'Later', label: 'Yes - but later' },
                { value: 'No', label: 'No' },
            ],
        },
    ];

    readonly visible = computed(() => visibleQuestions(this.questions, this.answers()));

    readonly outstanding = computed(() => countOutstanding(this.questions, this.answers()));

    readonly totalForFirstPayroll = computed(() => this.visible().filter((q) => q.phase !== 'later').length);

    readonly answeredForFirstPayroll = computed(() => this.totalForFirstPayroll() - this.outstanding());

    valueFor(ref: string): AnswerValue {
        return this.answers()[ref] ?? null;
    }

    noteFor(ref: string): string {
        return this.notes()[ref] ?? '';
    }

    onAnswered(change: AnswerChange): void {
        this.answers.update((a) => ({ ...a, [change.ref]: change.value }));
        this.notes.update((n) => ({ ...n, [change.ref]: change.note }));
    }

    /**
     * Recomputes the mid-month joiner example for the selected rule. Working days are counted for real
     * against August 2026 rather than assumed, so the number holds up if anyone checks it.
     */
    private prorationExample(value: AnswerValue): string | null {
        if (!value) {
            return null;
        }

        const daysInMonth = new Date(Date.UTC(SAMPLE_YEAR, SAMPLE_MONTH + 1, 0)).getUTCDate();
        const daysPaid = daysInMonth - SAMPLE_JOIN_DAY + 1;

        let workingDaysInMonth = 0;
        let workingDaysPaid = 0;
        for (let d = 1; d <= daysInMonth; d++) {
            const dow = new Date(Date.UTC(SAMPLE_YEAR, SAMPLE_MONTH, d)).getUTCDay();
            const isWeeklyOff = dow === 5 || dow === 6; // Friday, Saturday
            if (isWeeklyOff) {
                continue;
            }
            workingDaysInMonth++;
            if (d >= SAMPLE_JOIN_DAY) {
                workingDaysPaid++;
            }
        }

        let amount: number;
        let basis: string;

        switch (value) {
            case 'Fixed30':
                amount = (SAMPLE_SALARY / 30) * daysPaid;
                basis = `${daysPaid} of 30 days`;
                break;
            case 'ActualDays':
                amount = (SAMPLE_SALARY / daysInMonth) * daysPaid;
                basis = `${daysPaid} of ${daysInMonth} days`;
                break;
            case 'WorkingDays':
                amount = (SAMPLE_SALARY / workingDaysInMonth) * workingDaysPaid;
                basis = `${workingDaysPaid} of ${workingDaysInMonth} working days`;
                break;
            case 'NoProration':
                return `Someone joining on 16 August 2026 on a KD ${SAMPLE_SALARY.toFixed(3)} salary would receive the full KD ${SAMPLE_SALARY.toFixed(3)} for August.`;
            default:
                return null;
        }

        return `Someone joining on 16 August 2026 on a KD ${SAMPLE_SALARY.toFixed(3)} salary would receive KD ${amount.toFixed(3)} for August (${basis}).`;
    }
}
