import { Component, ElementRef, computed, effect, input, output, signal, untracked, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { RuleBaseObjectMetadataDto, RuleFunctionMetadataDto } from '@shared/service-proxies/service-proxies';
import { RuleFormulaError, parseFormula } from './rule-formula';

/** One entry in the autocomplete list. */
export interface FormulaSuggestion {
    /** What the user sees. */
    label: string;
    /** What replaces the token being typed. */
    insert: string;
    /** Where the caret lands after insertion, relative to the end of `insert`. Negative moves left. */
    caretOffset: number;
    kind: 'parameter' | 'field' | 'function' | 'variable' | 'keyword';
    /** Right-hand hint: a data type, a function signature, a keyword's meaning. */
    detail: string;
    /** Greyed-out second line, used for the writable marker and function descriptions. */
    description?: string;
}

/**
 * A formula box with autocomplete, driven entirely by the same metadata the tree editor uses.
 *
 * The interaction is the familiar one: type, get a filtered list, arrow down, Enter. Typing a parameter
 * name and then a dot switches the list to that parameter's fields, which is what makes the whole field
 * catalogue discoverable without a single dropdown -- the discoverability argument for the tree editor was
 * always its strongest, and this is how it gets kept.
 *
 * The box parses on every keystroke and shows the error under itself with the offending position. That is
 * the real usability difference: the tree editor could only tell you a rule was wrong when you saved it,
 * because a half-built tree is not a parseable thing. A half-typed formula is.
 *
 * The dropdown sits under the whole box rather than at the caret. Positioning a popup at a textarea's caret
 * needs a mirrored hidden div and breaks on wrap, scroll and zoom; under the box it is always right, and
 * for a two-line formula it is no further from where you are looking.
 */
@Component({
    selector: 'rule-formula-input',
    templateUrl: './rule-formula-input.component.html',
    styleUrls: ['./rule-formula-input.component.css'],
    imports: [FormsModule, LocalizePipe],
})
export class RuleFormulaInputComponent extends AppComponentBase {
    readonly value = input<string>('');
    readonly baseObject = input<RuleBaseObjectMetadataDto | undefined>(undefined);
    readonly functions = input<RuleFunctionMetadataDto[]>([]);
    readonly variableNames = input<string[]>([]);
    readonly placeholder = input<string>('');
    readonly rows = input<number>(2);

    /** Blank is legitimate for an optional condition, so emptiness is only an error where it is required. */
    readonly required = input<boolean>(false);

    readonly valueChange = output<string>();

    /** Emitted whenever the text parses or stops parsing, so the parent can block Save. */
    readonly validityChange = output<boolean>();

    readonly textarea = viewChild<ElementRef<HTMLTextAreaElement>>('editor');

    readonly text = signal<string>('');
    readonly caret = signal<number>(0);
    readonly isOpen = signal<boolean>(false);
    readonly highlightedIndex = signal<number>(0);
    readonly errorMessage = signal<string>('');
    readonly errorPosition = signal<number>(-1);

    /**
     * The last text this box and its parent agreed on.
     *
     * Without it the sync effect fights the typist: the effect would depend on `text`, so every keystroke
     * would re-run it, and it would compare against the parent's not-yet-updated `value` and write the old
     * text straight back. Comparing against what was last exchanged -- and reading `text` untracked --
     * makes the effect fire only when the PARENT genuinely changes the value, which is what it is for.
     */
    private lastSyncedValue = '';

    constructor() {
        super();

        // The parent owns the text; this keeps the box in step when a different rule is loaded into it.
        effect(() => {
            const incoming = this.value() ?? '';

            if (incoming === this.lastSyncedValue) {
                return;
            }

            this.lastSyncedValue = incoming;

            untracked(() => {
                this.text.set(incoming);
                this.validate(incoming);
            });
        });
    }

    /**
     * The identifier being typed at the caret. Everything the autocomplete does keys off this: dots are part
     * of it, so "Employee.Joi" arrives as one token and the list can tell it needs to offer fields.
     */
    readonly currentToken = computed<{ start: number; end: number; text: string }>(() => {
        const text = this.text();
        const caret = Math.min(this.caret(), text.length);

        let start = caret;
        while (start > 0 && /[A-Za-z0-9_.]/.test(text[start - 1])) {
            start--;
        }

        return { start, end: caret, text: text.substring(start, caret) };
    });

    readonly suggestions = computed<FormulaSuggestion[]>(() => {
        const token = this.currentToken().text;
        const dot = token.lastIndexOf('.');

        if (dot < 0) {
            return this.rootSuggestions(token);
        }

        // Everything before the last dot is a path -- "Leave.Employee.Nationality" -- and what follows is
        // the prefix being typed at that level.
        return this.pathSuggestions(token.substring(0, dot).split('.'), token.substring(dot + 1));
    });

    /** Signature help for the call the caret sits inside, shown above the box while typing arguments. */
    readonly activeSignature = computed<string>(() => {
        const text = this.text();
        const caret = Math.min(this.caret(), text.length);

        // Walk back counting brackets to find the call this caret belongs to.
        let depth = 0;

        for (let i = caret - 1; i >= 0; i--) {
            const ch = text[i];

            if (ch === ')') {
                depth++;
            } else if (ch === '(') {
                if (depth === 0) {
                    let start = i;
                    while (start > 0 && /[A-Za-z0-9_]/.test(text[start - 1])) {
                        start--;
                    }

                    const name = text.substring(start, i);
                    const descriptor = this.functions().find((f) => f.name.toLowerCase() === name.toLowerCase());

                    return descriptor ? this.signatureOf(descriptor) : '';
                }

                depth--;
            }
        }

        return '';
    });

    onInput(text: string): void {
        // Recorded BEFORE emitting, so the parent's echo of this same text is recognised as already agreed
        // and the sync effect stays out of the way while the user is still typing.
        this.lastSyncedValue = text;

        this.text.set(text);
        this.syncCaret();
        this.isOpen.set(true);
        this.highlightedIndex.set(0);
        this.validate(text);
        this.valueChange.emit(text);
    }

    onCaretMoved(): void {
        this.syncCaret();
    }

    onBlur(): void {
        // Delayed so a click on a suggestion lands before the list disappears.
        setTimeout(() => this.isOpen.set(false), 150);
    }

    onKeyDown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            this.isOpen.set(false);
            return;
        }

        // Ctrl+Space opens the list on demand, the shortcut every code editor uses.
        if (event.key === ' ' && (event.ctrlKey || event.metaKey)) {
            event.preventDefault();
            this.syncCaret();
            this.isOpen.set(true);
            this.highlightedIndex.set(0);
            return;
        }

        if (!this.isOpen() || this.suggestions().length === 0) {
            return;
        }

        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                this.highlightedIndex.set((this.highlightedIndex() + 1) % this.suggestions().length);
                break;

            case 'ArrowUp':
                event.preventDefault();
                this.highlightedIndex.set(
                    (this.highlightedIndex() - 1 + this.suggestions().length) % this.suggestions().length
                );
                break;

            case 'Enter':
            case 'Tab':
                // Enter only commits when the list is genuinely offering something for what is being typed;
                // otherwise it must still insert a newline, or a multi-line formula becomes untypeable.
                if (this.currentToken().text.length > 0 || event.key === 'Tab') {
                    event.preventDefault();
                    this.accept(this.suggestions()[this.highlightedIndex()]);
                }
                break;
        }
    }

    accept(suggestion: FormulaSuggestion): void {
        if (!suggestion) {
            return;
        }

        const token = this.currentToken();
        const text = this.text();
        const next = text.substring(0, token.start) + suggestion.insert + text.substring(token.end);
        const caret = token.start + suggestion.insert.length + suggestion.caretOffset;

        this.lastSyncedValue = next;
        this.text.set(next);
        this.validate(next);
        this.valueChange.emit(next);

        const element = this.textarea()?.nativeElement;

        if (element) {
            // Restored after the value binding has flushed, or the browser drops the caret to the end.
            setTimeout(() => {
                element.focus();
                element.setSelectionRange(caret, caret);
                this.caret.set(caret);
            });
        }

        // A parameter insert ends in a dot, so the list should stay open and immediately show its fields.
        this.isOpen.set(suggestion.kind === 'parameter');
        this.highlightedIndex.set(0);
    }

    private syncCaret(): void {
        const element = this.textarea()?.nativeElement;

        if (element) {
            this.caret.set(element.selectionStart ?? 0);
        }
    }

    private validate(text: string): void {
        if (!text || !text.trim()) {
            this.errorMessage.set(this.required() ? this.l('FormulaIsRequired') : '');
            this.errorPosition.set(-1);
            this.validityChange.emit(!this.required());
            return;
        }

        try {
            parseFormula(text);
            this.errorMessage.set('');
            this.errorPosition.set(-1);
            this.validityChange.emit(true);
        } catch (error) {
            const formulaError = error as RuleFormulaError;

            this.errorMessage.set(formulaError.message);
            this.errorPosition.set(formulaError.position ?? -1);
            this.validityChange.emit(false);
        }
    }

    private rootSuggestions(prefix: string): FormulaSuggestion[] {
        const lower = prefix.toLowerCase();
        const suggestions: FormulaSuggestion[] = [];
        const baseObject = this.baseObject();

        for (const parameter of baseObject?.parameters ?? []) {
            suggestions.push({
                label: parameter.name,
                insert: parameter.name + '.',
                caretOffset: 0,
                kind: 'parameter',
                detail: parameter.displayName,
                description: parameter.isWritable ? undefined : this.l('ReadOnly'),
            });
        }

        for (const fn of this.functions()) {
            suggestions.push({
                label: fn.name,
                insert: fn.name + '()',
                // Land between the brackets, ready for the first argument.
                caretOffset: -1,
                kind: 'function',
                detail: this.signatureOf(fn),
                description: fn.displayName,
            });
        }

        for (const name of this.variableNames()) {
            suggestions.push({
                label: name,
                insert: name,
                caretOffset: 0,
                kind: 'variable',
                detail: this.l('RuleVariable'),
            });
        }

        for (const keyword of ['AND', 'OR', 'NOT', 'IN', 'IS EMPTY', 'IS NOT EMPTY', 'CONTAINS', 'STARTS WITH', 'ENDS WITH', 'true', 'false']) {
            suggestions.push({
                label: keyword,
                insert: keyword,
                caretOffset: 0,
                kind: 'keyword',
                detail: '',
            });
        }

        return suggestions.filter((s) => !lower || s.label.toLowerCase().indexOf(lower) >= 0).slice(0, 60);
    }

    /**
     * Suggestions for a dotted path: walk the parameter, then each declared reference, and offer whatever
     * sits at the level the caret has reached.
     *
     * This is what makes <c>Leave.Employee.Nationality.NameEn</c> discoverable -- each dot moves one level
     * deeper into the same tree the server will walk when the rule runs, so the list can never offer a path
     * the evaluator would reject.
     */
    private pathSuggestions(pathSegments: string[], prefix: string): FormulaSuggestion[] {
        const parameter = this.baseObject()?.parameters.find(
            (p) => p.name.toLowerCase() === pathSegments[0]?.toLowerCase()
        );

        if (!parameter) {
            return [];
        }

        let fields = parameter.fields;
        let navigations = parameter.navigations ?? [];

        for (let i = 1; i < pathSegments.length; i++) {
            const segment = pathSegments[i].toLowerCase();
            const navigation = navigations.find((n) => n.name.toLowerCase() === segment);

            if (!navigation) {
                // A segment that is not a declared reference -- a typo, or a field with more typed after it.
                // Offering the top level again would be misleading, so offer nothing.
                return [];
            }

            fields = navigation.fields;
            navigations = navigation.navigations ?? [];
        }

        const lower = prefix.toLowerCase();
        const path = pathSegments.join('.');
        const suggestions: FormulaSuggestion[] = [];

        // References first: they lead somewhere, and a list that buried them under thirty fields would hide
        // the very thing this feature exists to expose.
        for (const navigation of navigations) {
            if (lower && navigation.name.toLowerCase().indexOf(lower) < 0 && navigation.displayName.toLowerCase().indexOf(lower) < 0) {
                continue;
            }

            suggestions.push({
                label: navigation.name,
                insert: path + '.' + navigation.name + '.',
                caretOffset: 0,
                kind: 'parameter',
                detail: navigation.displayName,
                description: this.l('RuleRelatedRecord'),
            });
        }

        for (const field of fields) {
            if (lower && field.name.toLowerCase().indexOf(lower) < 0 && field.displayName.toLowerCase().indexOf(lower) < 0) {
                continue;
            }

            suggestions.push({
                label: field.name,
                insert: path + '.' + field.name,
                caretOffset: 0,
                kind: 'field',
                detail: field.displayName,
                // Says why a field is missing from the assignment list, rather than leaving its absence
                // unexplained.
                description: field.isWritable ? undefined : this.l('ReadOnly'),
            });
        }

        return suggestions.slice(0, 60);
    }

    private signatureOf(fn: RuleFunctionMetadataDto): string {
        const args = fn.parameters.map((p) => (p.isOptional ? `[${p.name}]` : p.name)).join(', ');

        return `${fn.name}(${args})`;
    }

    iconFor(kind: string): string {
        switch (kind) {
            case 'parameter':
                return 'fa-cube';
            case 'field':
                return 'fa-tag';
            case 'function':
                return 'fa-superscript';
            case 'variable':
                return 'fa-dollar-sign';
            default:
                return 'fa-code';
        }
    }
}
