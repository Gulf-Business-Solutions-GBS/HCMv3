import { Directive, ElementRef, HostListener, OnInit, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * Every date field, typed and shown as dd/MM/yyyy -- whatever the browser's language is.
 *
 * A native date field takes its format from the browser, not from the page: the same screen shows dd/mm/yyyy to one
 * person and mm/dd/yyyy -- or a month typed as "Sep" -- to the next. This turns the field into a plain text box that
 * reads and writes dd/MM/yyyy everywhere, and keeps the calendar button: pressing it (or Alt+Down) hands over to the
 * browser's own picker for the moment it is open.
 *
 * The value passed in and out is unchanged: the ISO date the screens already bind (yyyy-MM-dd, yyyy-MM-ddTHH:mm for
 * a date and time, yyyy-MM for a month). Nothing in a template has to change beyond listing this directive in the
 * component. A month field reads mm/yyyy, for the same reason: the browser spells the month out as "September".
 */
@Directive({
    selector: 'input[type=date], input[type=datetime-local], input[type=month]',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => DateInputDirective),
            multi: true,
        },
    ],
})
export class DateInputDirective implements ControlValueAccessor, OnInit {
    private readonly element: HTMLInputElement = inject(ElementRef).nativeElement;

    /**
     * What the field was before it became a text box: a day, a day and a time, or a month on its own. It is also
     * what the picker is handed back when one is opened.
     */
    private kind: FieldKind = 'date';

    /** The last value handed out, in ISO. Kept so the picker can open on it and so blur can restore it. */
    private iso: string | null = null;

    /**
     * What the field held after the last keystroke it accepted. A keystroke that could only make an impossible date --
     * a 22nd month -- is undone back to this, so the field never shows a date it will not hand over.
     */
    private lastText = '';

    /** True only while the browser's own picker is up, when the field briefly holds a native value. */
    private picking = false;

    private propagateChange: (value: string | null) => void = () => undefined;
    private propagateTouched: () => void = () => undefined;

    ngOnInit(): void {
        this.kind = FieldKinds.includes(this.element.type as FieldKind) ? (this.element.type as FieldKind) : 'date';

        this.element.type = 'text';
        this.element.autocomplete = 'off';
        this.element.setAttribute('inputmode', 'numeric');
        this.element.maxLength = Lengths[this.kind];

        if (!this.element.placeholder) {
            this.element.placeholder = Placeholders[this.kind];
        }

        // The calendar button, drawn on the field itself rather than by wrapping it in more markup: a wrapper would
        // sit between Angular and the element every screen already styles and measures.
        this.element.style.backgroundImage = CalendarIcon;
        this.element.style.backgroundRepeat = 'no-repeat';
        this.element.style.backgroundPosition = 'right 0.65rem center';
        this.element.style.backgroundSize = '16px 16px';
        this.element.style.paddingRight = '2.1rem';
    }

    // ------------------------------------------------------------------ ControlValueAccessor

    writeValue(value: unknown): void {
        this.iso = normalizeIso(value, this.kind);
        this.element.value = this.iso ? display(this.iso, this.kind) : '';
        this.lastText = this.element.value;
        this.element.classList.remove(IncompleteClass);
    }

    registerOnChange(fn: (value: string | null) => void): void {
        this.propagateChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.propagateTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        this.element.disabled = isDisabled;
    }

    // ------------------------------------------------------------------ typing

    @HostListener('input')
    onInput(): void {
        if (this.picking) {
            // A value straight from the picker is already ISO.
            this.commit(this.element.value || null);
            return;
        }

        const caret = this.element.selectionStart ?? this.element.value.length;
        const caretAtEnd = caret === this.element.value.length;

        let typed = this.element.value;
        if (caretAtEnd) {
            // Separators as they type, so nobody has to reach for the slash key.
            typed = punctuate(typed, this.kind);
        }

        // A part that can no longer come good is refused at the keystroke that made it, rather than accepted and
        // quietly dropped at the end. Typing 01222026 used to leave "01/22/2026" sitting in the field -- a 22nd month,
        // so nothing was handed over, and the form then complained that the date was missing while showing one.
        if (impossible(typed, this.kind)) {
            this.element.value = this.lastText;
            const at = Math.max(0, Math.min(caret - 1, this.lastText.length));
            this.element.setSelectionRange(at, at);
            return;
        }

        if (typed !== this.element.value) {
            this.element.value = typed;
        }

        this.lastText = this.element.value;

        // Nothing is marked wrong mid-word: "01/2" is a perfectly good start. Blur is where a field that never became
        // a date says so.
        this.element.classList.remove(IncompleteClass);

        this.commit(parse(this.element.value, this.kind));
    }

    @HostListener('blur')
    onBlur(): void {
        if (this.picking) {
            this.stopPicking();
        }

        // Tidied to the canonical form once they leave: "9/9/2026" typed in a hurry comes back as 09/09/2026.
        if (this.iso) {
            this.element.value = display(this.iso, this.kind);
        }

        this.lastText = this.element.value;

        // Half a date left behind -- "01/2", or a 31st of February -- is marked here. Without it the field looks
        // filled while holding nothing, and the only hint is a "required" message on a field that plainly has text.
        this.element.classList.toggle(IncompleteClass, !!this.element.value && !this.iso);

        this.propagateTouched();
    }

    // ------------------------------------------------------------------ the browser's picker

    @HostListener('click', ['$event'])
    onClick(event: MouseEvent): void {
        // Only the calendar button at the end of the field; a click anywhere else is somebody placing the cursor.
        const withinButton = event.offsetX > this.element.clientWidth - 34;
        if (withinButton && !this.element.disabled && !this.element.readOnly) {
            this.openPicker();
        }
    }

    @HostListener('keydown', ['$event'])
    onKeyDown(event: KeyboardEvent): void {
        if (event.altKey && event.key === 'ArrowDown') {
            event.preventDefault();
            this.openPicker();
        }
    }

    @HostListener('change')
    onChange(): void {
        if (this.picking) {
            this.commit(this.element.value || null);
            this.stopPicking();
        }
    }

    private openPicker(): void {
        if (this.picking) {
            return;
        }

        this.picking = true;
        this.element.type = this.kind;
        this.element.value = this.iso ?? '';

        // The native field brings a calendar button of its own for as long as it is up; two would be one too many.
        this.element.style.backgroundImage = 'none';

        try {
            (this.element as unknown as { showPicker?: () => void }).showPicker?.();
        } catch {
            // Refused (an old browser, or no user gesture behind this): the field is native for the moment, which is
            // its own way of picking a date, and blur puts it back.
        }
    }

    private stopPicking(): void {
        this.picking = false;
        this.element.type = 'text';
        this.element.style.backgroundImage = CalendarIcon;
        this.element.value = this.iso ? display(this.iso, this.kind) : '';
        this.lastText = this.element.value;
    }

    /** Hands out an ISO value, or null while what is typed is not yet a date -- which is what makes "required" fire. */
    private commit(iso: string | null): void {
        const bounded = withinMinMax(iso, this.element) ? iso : null;
        if (bounded === this.iso) {
            return;
        }

        this.iso = bounded;
        this.propagateChange(bounded);
    }
}

/** The three native fields this stands in for: a day, a day and a time, and a month on its own. */
type FieldKind = 'date' | 'datetime-local' | 'month';

const FieldKinds: FieldKind[] = ['date', 'datetime-local', 'month'];

const Lengths: Record<FieldKind, number> = { date: 10, 'datetime-local': 16, month: 7 };

const Placeholders: Record<FieldKind, string> = {
    date: 'dd/mm/yyyy',
    'datetime-local': 'dd/mm/yyyy hh:mm',
    month: 'mm/yyyy',
};

/** A calendar, drawn in the field. Grey enough to read as a button without competing with the text. */
const CalendarIcon =
    "url(\"data:image/svg+xml;charset=utf-8,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16' fill='none' stroke='%237e8299' stroke-width='1.3'%3E%3Crect x='1.6' y='2.8' width='12.8' height='11.6' rx='1.6'/%3E%3Cpath d='M1.6 6.2h12.8M5 1.6v2.4M11 1.6v2.4'/%3E%3C/svg%3E\")";

function pad(value: number, length = 2): string {
    return value.toString().padStart(length, '0');
}

/**
 * What arrives from a screen: the ISO string they bind, or occasionally a Date. Only the shape matters here -- the
 * value is handed straight back out in the same form, never reinterpreted in a time zone.
 */
function normalizeIso(value: unknown, kind: FieldKind): string | null {
    if (value === null || value === undefined || value === '') {
        return null;
    }

    if (value instanceof Date) {
        const month = `${value.getFullYear()}-${pad(value.getMonth() + 1)}`;
        if (kind === 'month') {
            return month;
        }

        const date = `${month}-${pad(value.getDate())}`;
        return kind === 'datetime-local' ? `${date}T${pad(value.getHours())}:${pad(value.getMinutes())}` : date;
    }

    const text = String(value);
    return text.substring(0, kind === 'month' ? 7 : kind === 'datetime-local' ? 16 : 10);
}

function display(iso: string, kind: FieldKind): string {
    const [datePart, timePart] = iso.split('T');
    const [year, month, day] = datePart.split('-');
    if (!year || !month) {
        return '';
    }

    if (kind === 'month') {
        return `${month}/${year}`;
    }

    if (!day) {
        return '';
    }

    const date = `${day}/${month}/${year}`;
    return kind === 'datetime-local' && timePart ? `${date} ${timePart.substring(0, 5)}` : date;
}

/**
 * Slashes and the colon put in as they type, so the field fills the way the format reads.
 *
 * A separator they typed themselves ends that part, short or not: "9/9/2026" is the 9th of September, not the digits
 * 992026 read off in fours. Only the year is left alone -- a year is the one part nobody writes short and means.
 */
function punctuate(text: string, kind: FieldKind): string {
    // Where the year starts, and so the one part a lone digit is never padded into.
    const yearAt = kind === 'month' ? 2 : 4;

    let digits = '';
    let part = '';

    for (const character of text) {
        if (character >= '0' && character <= '9') {
            part += character;
            continue;
        }

        if (part.length === 1 && digits.length !== yearAt) {
            part = '0' + part;
        }

        digits += part;
        part = '';
    }

    digits = (digits + part).substring(0, kind === 'month' ? 6 : kind === 'datetime-local' ? 12 : 8);

    if (kind === 'month') {
        return digits.length > 2 ? digits.substring(0, 2) + '/' + digits.substring(2, 6) : digits;
    }

    let out = digits.substring(0, 2);
    if (digits.length > 2) {
        out += '/' + digits.substring(2, 4);
    }
    if (digits.length > 4) {
        out += '/' + digits.substring(4, 8);
    }
    if (kind === 'datetime-local' && digits.length > 8) {
        out += ' ' + digits.substring(8, 10);
    }
    if (kind === 'datetime-local' && digits.length > 10) {
        out += ':' + digits.substring(10, 12);
    }

    return out;
}

/** Put on a field left holding something that is not a date, so it is obvious before anybody presses Save. */
const IncompleteClass = 'is-invalid';

/**
 * Whether what is now in the field can never become a date, so the keystroke behind it should be refused.
 *
 * Judged only on parts that are finished -- two digits for a day, a month, an hour or a minute, four for a year. A
 * part still being typed is left alone: "3" is the start of the 3rd or the 31st, and "1" of January, October,
 * November or December. Whether the day exists in that particular month is a separate question, answered by
 * <c>parse</c> once the month is known, because 31 is only wrong once February is typed beside it.
 */
function impossible(text: string, kind: FieldKind): boolean {
    const [datePart, timePart] = text.split(' ');
    const parts = datePart.split('/');

    if (kind === 'month') {
        return outOfRange(parts[0], 1, 12) || outOfRange(parts[1], 1, 9999, 4);
    }

    if (outOfRange(parts[0], 1, 31) || outOfRange(parts[1], 1, 12) || outOfRange(parts[2], 1, 9999, 4)) {
        return true;
    }

    if (kind !== 'datetime-local' || !timePart) {
        return false;
    }

    const [hour, minute] = timePart.split(':');
    return outOfRange(hour, 0, 23) || outOfRange(minute, 0, 59);
}

/** True once a part is as long as it will get and still outside what it could mean. */
function outOfRange(part: string | undefined, low: number, high: number, length = 2): boolean {
    if (part === undefined || part.length < length) {
        return false;
    }

    const value = Number(part);
    return !Number.isNaN(value) && (value < low || value > high);
}

/**
 * dd/MM/yyyy, and the ways people actually type it: dashes, dots, or nothing at all between the parts. A two-digit
 * year is refused rather than guessed -- "80" in a date of birth is not 2080, and nobody would notice the difference
 * until a length of service came out a century short.
 */
function parse(text: string, kind: FieldKind): string | null {
    const trimmed = (text ?? '').trim();
    if (!trimmed) {
        return null;
    }

    if (kind === 'month') {
        const monthMatch = trimmed.match(/^(\d{1,2})[\/\-. ]?(\d{4})$/);
        if (!monthMatch) {
            return null;
        }

        const onlyMonth = Number(monthMatch[1]);
        return onlyMonth >= 1 && onlyMonth <= 12 ? `${monthMatch[2]}-${pad(onlyMonth)}` : null;
    }

    const match = trimmed.match(/^(\d{1,2})[\/\-. ]?(\d{1,2})[\/\-. ]?(\d{4})(?:[ T,]+(\d{1,2}):?(\d{2}))?$/);
    if (!match) {
        return null;
    }

    const day = Number(match[1]);
    const month = Number(match[2]);
    const year = Number(match[3]);

    // A real day of a real month: 31/02 is not a date, however well it is spelled.
    const asDate = new Date(year, month - 1, day);
    if (asDate.getFullYear() !== year || asDate.getMonth() !== month - 1 || asDate.getDate() !== day) {
        return null;
    }

    const iso = `${pad(year, 4)}-${pad(month)}-${pad(day)}`;
    if (kind !== 'datetime-local') {
        return iso;
    }

    const hour = match[4] === undefined ? 0 : Number(match[4]);
    const minute = match[5] === undefined ? 0 : Number(match[5]);
    if (hour > 23 || minute > 59) {
        return null;
    }

    return `${iso}T${pad(hour)}:${pad(minute)}`;
}

/** The min and max a screen set on the field still hold: they were the browser's job until the field became text. */
function withinMinMax(iso: string | null, element: HTMLInputElement): boolean {
    if (!iso) {
        return true;
    }

    const min = bound(element.getAttribute('min'));
    const max = bound(element.getAttribute('max'));

    return (!min || iso >= min) && (!max || iso <= max);
}

/**
 * A min or max worth holding a date to: one that is itself a date. A screen that binds <c>[min]="nothing"</c> leaves
 * the attribute holding the word "null", and comparing against that refused every date typed into the field.
 */
function bound(value: string | null): string | null {
    return value && /^\d{4}-\d{2}/.test(value) ? value : null;
}
