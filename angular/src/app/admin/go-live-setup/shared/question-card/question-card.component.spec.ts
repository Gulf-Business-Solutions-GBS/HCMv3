import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LocalizationService } from 'abp-ng2-module';
import { QuestionCardComponent } from './question-card.component';
import { AnswerMap, GoLiveQuestion, countOutstanding, isAnswered, visibleQuestions } from './go-live-question.types';

describe('go-live-question helpers', () => {
    const questions: GoLiveQuestion[] = [
        { ref: 'A', question: 'a', phase: 'first-payroll' },
        { ref: 'B', question: 'b', phase: 'first-payroll', showWhen: (ans) => ans['A'] === 'yes' },
        { ref: 'C', question: 'c', phase: 'later' },
    ];

    it('should treat empty string and null as unanswered', () => {
        expect(isAnswered(null)).toBe(false);
        expect(isAnswered('')).toBe(false);
        expect(isAnswered(0)).toBe(true);
        expect(isAnswered('No')).toBe(true);
    });

    it('should hide a question whose condition is not met', () => {
        expect(visibleQuestions(questions, {}).map((q) => q.ref)).toEqual(['A', 'C']);
    });

    it('should reveal a question once its condition is met', () => {
        expect(visibleQuestions(questions, { A: 'yes' }).map((q) => q.ref)).toEqual(['A', 'B', 'C']);
    });

    it('should count only visible first-payroll questions as outstanding', () => {
        expect(countOutstanding(questions, {})).toBe(1); // A only; B hidden, C is phase 'later'
        expect(countOutstanding(questions, { A: 'yes' })).toBe(1); // A answered, B now visible
        expect(countOutstanding(questions, { A: 'yes', B: 'x' })).toBe(0);
    });

    it('should not count a hidden question the customer can never reach', () => {
        const answers: AnswerMap = { A: 'no' };
        expect(countOutstanding(questions, answers)).toBe(0);
    });
});

/** Mounts the real component, which also proves the template compiles under strict template checking. */
describe('QuestionCardComponent', () => {
    let fixture: ComponentFixture<QuestionCardComponent>;
    let card: QuestionCardComponent;

    function mount(question: GoLiveQuestion): void {
        fixture = TestBed.createComponent(QuestionCardComponent);
        card = fixture.componentInstance;
        fixture.componentRef.setInput('question', question);
        fixture.componentRef.setInput('answers', {});
        fixture.detectChanges();
    }

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [QuestionCardComponent],
            providers: [{ provide: LocalizationService, useValue: { localize: (k: string) => k } }],
        }).compileComponents();
    });

    it('should render radios for a short choice list', () => {
        mount({
            ref: 'Q1',
            question: 'Pick one',
            control: 'choice',
            options: [
                { value: 'a', label: 'A' },
                { value: 'b', label: 'B' },
            ],
        });

        expect(card.useRadios()).toBe(true);
        expect(fixture.nativeElement.querySelectorAll('input[type="radio"]').length).toBe(2);
    });

    it('should fall back to a select once the list is longer than four', () => {
        mount({
            ref: 'Q1',
            question: 'Pick one',
            control: 'choice',
            options: ['a', 'b', 'c', 'd', 'e'].map((v) => ({ value: v, label: v.toUpperCase() })),
        });

        expect(card.useRadios()).toBe(false);
        expect(fixture.nativeElement.querySelector('select')).toBeTruthy();
    });

    it('should offer Yes and No for a yesno question', () => {
        mount({ ref: 'Q1', question: 'Really?', control: 'yesno' });

        expect(card.radioOptions().map((o) => o.value)).toEqual(['Yes', 'No']);
    });

    it('should emit the answer with its ref', () => {
        mount({ ref: 'Q2.04', question: 'Pick one', control: 'choice', options: [{ value: 'a', label: 'A' }] });

        const seen: string[] = [];
        card.answered.subscribe((c) => seen.push(`${c.ref}=${c.value}`));
        card.setValue('a');

        expect(seen).toEqual(['Q2.04=a']);
    });

    it('should mark itself answered only once a value is set', () => {
        mount({ ref: 'Q1', question: 'Anything', control: 'text' });
        expect(card.isAnswered()).toBe(false);

        card.setValue('something');
        expect(card.isAnswered()).toBe(true);
    });

    it('should treat a cleared number input as unanswered rather than zero', () => {
        mount({ ref: 'Q1', question: 'How many?', control: 'number' });

        card.onNumberInput({ target: { value: '' } } as unknown as Event);
        expect(card.value()).toBeNull();

        card.onNumberInput({ target: { value: '0' } } as unknown as Event);
        expect(card.value()).toBe(0);
    });

    it('should show no worked example until the question is answered', () => {
        mount({
            ref: 'Q2.04',
            question: 'Pick one',
            control: 'choice',
            options: [{ value: 'Fixed30', label: 'By 30' }],
            preview: (v) => (v ? `You picked ${v}` : null),
        });

        expect(card.previewText()).toBeNull();

        card.setValue('Fixed30');
        fixture.detectChanges();

        expect(card.previewText()).toBe('You picked Fixed30');
        expect(fixture.nativeElement.querySelector('.qc-preview')).toBeTruthy();
    });

    it('should keep the note box open once it holds text', () => {
        mount({ ref: 'Q1', question: 'Anything' });
        expect(card.showNoteBox()).toBe(false);

        card.onNoteInput({ target: { value: 'our rule differs by grade' } } as unknown as Event);
        expect(card.showNoteBox()).toBe(true);
    });

    it('should emit the note alongside the value', () => {
        mount({ ref: 'Q1', question: 'Anything' });

        const seen: { value: unknown; note: string }[] = [];
        card.answered.subscribe((c) => seen.push(c));
        card.setValue('yes');
        card.onNoteInput({ target: { value: 'caveat' } } as unknown as Event);

        const last = seen[seen.length - 1];
        expect(last.note).toBe('caveat');
        expect(last.value).toBe('yes');
    });

    it('should hide the note box when the question opts out', () => {
        mount({ ref: 'Q1', question: 'Anything', allowNote: false });

        expect(fixture.nativeElement.querySelector('.qc-note-toggle')).toBeNull();
    });
});
