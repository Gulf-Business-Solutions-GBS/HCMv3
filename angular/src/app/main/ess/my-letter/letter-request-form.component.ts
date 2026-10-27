import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EssLookupServiceProxy, SimpleLookupDto } from '@shared/service-proxies/service-proxies';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';
import { EssRequestFormMode } from '../my-exit-permit/exit-permit-form.component';

/**
 * The letter request as the form holds it. The date is ISO <c>yyyy-MM-dd</c>, because that is what the date
 * input reads and writes.
 */
export interface LetterRequestFormValue {
    /** "1042 - Ahmad Al Sayed". Shown, never chosen. */
    employeeDisplay: string;

    /** The saved request, when it is being read: the printed letter is kept on it as a file. */
    id?: number;

    documentTypeId: number | null;

    /**
     * The words to show where the picker cannot be used: read-only, or a draft whose letter type is no longer
     * in the list.
     */
    documentTypeLabel?: string;

    effectiveDate: string;
    addressToEn: string;
    addressToAr: string;
    comments: string;
}

/** An empty form. */
export function emptyLetterRequestForm(employeeDisplay = ''): LetterRequestFormValue {
    return {
        employeeDisplay,
        documentTypeId: null,
        effectiveDate: '',
        addressToEn: '',
        addressToAr: '',
        comments: '',
    };
}

/**
 * The letter request form -- the SAME form whether an employee is filling it in, looking at what they sent, or
 * an approver is deciding on it, exactly as the exit permit form is. Read-only means disabled, not rearranged:
 * every field keeps its place.
 *
 * <p>The type list is fetched with only the letter types in it, so a passport or a residency permit never
 * appears as something that can be "requested as a letter". The server checks the same thing again: a
 * filtered dropdown is a convenience, not a rule.</p>
 */
@Component({
    selector: 'letter-request-form',
    templateUrl: './letter-request-form.component.html',
    imports: [DateInputDirective, CommonModule, FormsModule, LocalizePipe, AttachmentsComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LetterRequestFormComponent extends AppComponentBase implements OnInit {
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly mode = input<EssRequestFormMode>('create');

    /** The form's data. In create mode the host passes its own object and this writes into it through ngModel. */
    readonly value = input.required<LetterRequestFormValue>();

    /** Disabled while the host is saving, so a double Submit cannot raise two requests. */
    readonly saving = input(false);

    /** Save as draft: files the request without sending it to an approver. */
    readonly saved = output<void>();
    readonly submitted = output<void>();
    readonly cancelled = output<void>();

    readonly readOnly = computed(() => this.mode() !== 'create');

    readonly letterTypes = signal<SimpleLookupDto[]>([]);

    ngOnInit(): void {
        if (this.readOnly()) {
            return;
        }

        this._lookups.getLetterTypes().subscribe({
            next: (result) => this.letterTypes.set(result ?? []),
            error: () => this.letterTypes.set([]),
        });
    }

    letterTypeName(type: SimpleLookupDto): string {
        return this.isRtl ? type.nameAr || type.nameEn || '' : type.nameEn || '';
    }

    /**
     * A saved draft's letter type when the picker's list does not hold it (or has not arrived yet), so the
     * draft still shows what it was saved with rather than an empty picker.
     */
    unlistedTypeLabel(): string {
        const id = this.value().documentTypeId;

        return id && !this.letterTypes().some((type) => type.id === id) ? this.value().documentTypeLabel || '' : '';
    }

    /** A stored date shown the way the rest of the system shows one, as text so no time zone can move it. */
    asDayMonthYear(iso: string | undefined): string {
        const [year, month, day] = (iso ?? '').split('-');

        // The year 1 is a date that was never set, not a date.
        return year && month && day && Number(year) > 1 ? `${day.substring(0, 2)}/${month}/${year}` : '';
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
