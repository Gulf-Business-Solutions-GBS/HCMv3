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
 * The value passed in and out is unchanged: the ISO date the screens already bind (yyyy-MM-dd, or yyyy-MM-ddTHH:mm
 * for a date and time). Nothing in a template has to change beyond listing this directive in the component.
 */
@Directive({
    selector: 'input[type=date], input[type=datetime-local]',
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

    /** "date" or "datetime-local": what the field was before it became a text box, and what the picker needs back. */
    private nativeType = 'date';
    private withTime = false;

    /** The last value handed out, in ISO. Kept so the picker can open on it and so blur can restore it. */
    private iso: string | null = null;

    /** True only while the browser's own picker is up, when the field briefly holds a native value. */
    private picking = false;

    private propagateChange: (value: string | null) => void = () => undefined;
    private propagateTouched: () => void = () => undefined;

    ngOnInit(): void {
        this.nativeType = this.element.type === 'datetime-local' ? 'datetime-local' : 'date';
        this.withTime = this.nativeType === 'datetime-local';

        this.element.type = 'text';
        this.element.autocomplete = 'off';
        this.element.setAttribute('inputmode', 'numeric');
        this.element.maxLength = this.withTime ? 16 : 10;

        if (!this.element.placeholder) {
            this.element.placeholder = this.withTime ? 'dd/mm/yyyy hh:mm' : 'dd/mm/yyyy';
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
        this.iso = normalizeIso(value, this.withTime);
        this.element.value = this.iso ? display(this.iso, this.withTime) : '';
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

        const caretAtEnd = this.element.selectionStart === this.element.value.length;
        if (caretAtEnd) {
            // Separators as they type, so nobody has to reach for the slash key.
            const typed = punctuate(this.element.value, this.withTime);
            if (typed !== this.element.value) {
                this.element.value = typed;
            }
        }

        this.commit(parse(this.element.value, this.withTime));
    }

    @HostListener('blur')
    onBlur(): void {
        if (this.picking) {
            this.stopPicking();
        }

        // Tidied to the canonical form once they leave: "9/9/2026" typed in a hurry comes back as 09/09/2026.
        if (this.iso) {
            this.element.value = display(this.iso, this.withTime);
        }

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
        this.element.type = this.nativeType;
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
        this.element.value = this.iso ? display(this.iso, this.withTime) : '';
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
function normalizeIso(value: unknown, withTime: boolean): string | null {
    if (value === null || value === undefined || value === '') {
        return null;
    }

    if (value instanceof Date) {
        const date = `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}`;
        return withTime ? `${date}T${pad(value.getHours())}:${pad(value.getMinutes())}` : date;
    }

    const text = String(value);
    return withTime ? text.substring(0, 16) : text.substring(0, 10);
}

function display(iso: string, withTime: boolean): string {
    const [datePart, timePart] = iso.split('T');
    const [year, month, day] = datePart.split('-');
    if (!year || !month || !day) {
        return '';
    }

    const date = `${day}/${month}/${year}`;
    return withTime && timePart ? `${date} ${timePart.substring(0, 5)}` : date;
}

/**
 * Slashes and the colon put in as they type, so the field fills the way the format reads.
 *
 * A separator they typed themselves ends that part, short or not: "9/9/2026" is the 9th of September, not the digits
 * 992026 read off in fours. Only the year is left alone -- a year is the one part nobody writes short and means.
 */
function punctuate(text: string, withTime: boolean): string {
    let digits = '';
    let part = '';

    for (const character of text) {
        if (character >= '0' && character <= '9') {
            part += character;
            continue;
        }

        if (part.length === 1 && digits.length !== 4) {
            part = '0' + part;
        }

        digits += part;
        part = '';
    }

    digits = (digits + part).substring(0, withTime ? 12 : 8);

    let out = digits.substring(0, 2);
    if (digits.length > 2) {
        out += '/' + digits.substring(2, 4);
    }
    if (digits.length > 4) {
        out += '/' + digits.substring(4, 8);
    }
    if (withTime && digits.length > 8) {
        out += ' ' + digits.substring(8, 10);
    }
    if (withTime && digits.length > 10) {
        out += ':' + digits.substring(10, 12);
    }

    return out;
}

/**
 * dd/MM/yyyy, and the ways people actually type it: dashes, dots, or nothing at all between the parts. A two-digit
 * year is refused rather than guessed -- "80" in a date of birth is not 2080, and nobody would notice the difference
 * until a length of service came out a century short.
 */
function parse(text: string, withTime: boolean): string | null {
    const trimmed = (text ?? '').trim();
    if (!trimmed) {
        return null;
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
    if (!withTime) {
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

    const min = element.getAttribute('min');
    const max = element.getAttribute('max');

    return (!min || iso >= min) && (!max || iso <= max);
}
