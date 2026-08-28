import { Component, computed, input, model, output, signal } from '@angular/core';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AnswerChange, AnswerMap, AnswerValue, GoLiveQuestion, isAnswered } from './go-live-question.types';

/**
 * Renders one Go-Live Setup question.
 *
 * Every question in steps S1.1 - S1.7 goes through this component; the step components supply
 * descriptors and own the answer map. That is what keeps Go-Live Setup configuration-driven rather than
 * twenty hand-built forms.
 *
 * Three things here exist specifically because the offline workbook could not do them:
 *  - `preview`, a worked example recomputed live, so an abstract rule becomes a number;
 *  - `showWhen` (evaluated by the host), so irrelevant questions disappear instead of being skipped;
 *  - a per-question note, which is where "reality is more complicated than these options" gets captured
 *    rather than lost in an email thread.
 *
 * Member order is dictated by @typescript-eslint/member-ordering.
 */
@Component({
    selector: 'app-question-card',
    templateUrl: './question-card.component.html',
    imports: [LocalizePipe],
    styleUrl: './question-card.component.css',
})
export class QuestionCardComponent {
    readonly question = input.required<GoLiveQuestion>();

    /** Every answer in the step, so preview() can read sibling answers. */
    readonly answers = input<AnswerMap>({});

    readonly value = model<AnswerValue>(null);
    readonly note = model<string>('');

    readonly answered = output<AnswerChange>();

    readonly noteOpen = signal(false);

    readonly control = computed(() => this.question().control ?? 'text');

    readonly isAnswered = computed(() => isAnswered(this.value()));

    /** Radios show every option at once, which suits "review rather than compose". Beyond four the
     *  list gets taller than the question and a select reads better. */
    readonly useRadios = computed(() => {
        const q = this.question();
        return q.control === 'yesno' || (q.control === 'choice' && (q.options?.length ?? 0) <= 4);
    });

    readonly radioOptions = computed(() => {
        const q = this.question();
        if (q.control === 'yesno') {
            return [
                { value: 'Yes', label: 'Yes', hint: undefined },
                { value: 'No', label: 'No', hint: undefined },
            ];
        }
        return q.options ?? [];
    });

    readonly previewText = computed(() => {
        const q = this.question();
        if (!q.preview) {
            return null;
        }
        return q.preview(this.value(), this.answers());
    });

    readonly showNoteBox = computed(() => this.noteOpen() || this.note().length > 0);

    setValue(next: AnswerValue): void {
        this.value.set(next);
        this.emit();
    }

    onTextInput(event: Event): void {
        this.setValue((event.target as HTMLInputElement | HTMLTextAreaElement).value);
    }

    onNumberInput(event: Event): void {
        const raw = (event.target as HTMLInputElement).value;
        this.setValue(raw === '' ? null : Number(raw));
    }

    onSelectChange(event: Event): void {
        const raw = (event.target as HTMLSelectElement).value;
        this.setValue(raw === '' ? null : raw);
    }

    onNoteInput(event: Event): void {
        this.note.set((event.target as HTMLTextAreaElement).value);
        this.emit();
    }

    openNote(): void {
        this.noteOpen.set(true);
    }

    private emit(): void {
        this.answered.emit({ ref: this.question().ref, value: this.value(), note: this.note() });
    }
}
