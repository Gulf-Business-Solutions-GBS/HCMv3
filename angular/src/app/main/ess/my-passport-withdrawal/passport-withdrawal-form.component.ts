import {
    ChangeDetectionStrategy,
    Component,
    OnInit,
    computed,
    effect,
    inject,
    input,
    output,
    signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    EssColleagueLookupDto,
    EssLookupServiceProxy,
    EssWithdrawableDocumentDto,
    WorkflowRequestAction,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { EssRequestFormMode } from '../my-exit-permit/exit-permit-form.component';

/**
 * The passport withdrawal as the form holds it, for both of its requests: asking for the document out, and
 * reporting it handed back. Dates are ISO <c>yyyy-MM-dd</c>, because that is what the date inputs read and
 * write.
 */
export interface PassportWithdrawalFormValue {
    /** New for a withdrawal, Return for handing the document back. */
    action: WorkflowRequestAction;

    /** "1042 - Ahmad Al Sayed". Shown, never chosen. */
    employeeDisplay: string;

    employeeDocumentId: number | null;
    withdrawDate: string;
    returnDate: string;
    guarantorId: number | null;
    withdrawalReason: string;

    /** Request Comment on a withdrawal. */
    comments: string;

    /** A return only: the day the document came back, and what was noted when it did. */
    actualReturnDate: string;
    returnRemarks: string;

    /**
     * The words to show where a picker cannot be used -- read-only, a return, or a draft whose document or
     * guarantor is no longer in the list -- and the chosen document's own details.
     */
    documentLabel?: string;
    documentNumber?: string;
    issueDate?: string;
    expiryDate?: string;
    guarantorLabel?: string;

    /** The day the document was actually handed over, once it has been. */
    handedOverOn?: string;
}

/** An empty form for the given action. */
export function emptyPassportWithdrawalForm(
    action = WorkflowRequestAction.New,
    employeeDisplay = ''
): PassportWithdrawalFormValue {
    return {
        action,
        employeeDisplay,
        employeeDocumentId: null,
        withdrawDate: '',
        returnDate: '',
        guarantorId: null,
        withdrawalReason: '',
        comments: '',
        actualReturnDate: '',
        returnRemarks: '',
    };
}

/**
 * The passport withdrawal request form -- the SAME form whether an employee is filling it in, looking at what
 * they sent, or an approver is deciding on it, exactly as the exit permit form is. Read-only means disabled,
 * not rearranged: every field keeps its place.
 *
 * <p>One form serves both requests. A withdrawal lets the document, the dates, the reason and the guarantor be
 * entered. A return shows that withdrawal as it stands and asks only for the day the document came back and
 * any remarks; it has no draft, so it offers Submit and Cancel only.</p>
 */
@Component({
    selector: 'passport-withdrawal-form',
    templateUrl: './passport-withdrawal-form.component.html',
    imports: [DateInputDirective, CommonModule, FormsModule, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PassportWithdrawalFormComponent extends AppComponentBase implements OnInit {
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly mode = input<EssRequestFormMode>('create');

    /** The form's data. In create mode the host passes its own object and this writes into it through ngModel. */
    readonly value = input.required<PassportWithdrawalFormValue>();

    /** Disabled while the host is saving, so a double Submit cannot raise two requests. */
    readonly saving = input(false);

    /**
     * The documents already with the employee: an approved withdrawal that has not come back. Asking for one
     * of them again is an extension, and the form says so before it is sent. Only the list screen knows this.
     */
    readonly documentsOut = input<number[]>([]);

    /** Save as draft: files the request without sending it to an approver. */
    readonly saved = output<void>();
    readonly submitted = output<void>();
    readonly cancelled = output<void>();

    readonly readOnly = computed(() => this.mode() !== 'create');

    /** The documents the employee may withdraw -- the server decides which, so the picker offers nothing else. */
    readonly documents = signal<EssWithdrawableDocumentDto[]>([]);
    readonly documentsLoaded = signal(false);

    /** Colleagues who may be named as guarantor -- the same active-colleague list delegations use. */
    readonly guarantorCandidates = signal<EssColleagueLookupDto[]>([]);

    /** Bumped when this component fills a field in by itself, so the template notices. */
    private readonly revision = signal(0);

    constructor() {
        super();

        // Nothing to choose between when there is only one. Run when the list arrives and whenever the host
        // hands over a new form, whichever comes last.
        effect(() => {
            const value = this.value();
            const documents = this.documents();

            if (
                !this.readOnly() &&
                value.action === WorkflowRequestAction.New &&
                documents.length === 1 &&
                !value.employeeDocumentId
            ) {
                value.employeeDocumentId = documents[0].id;
                this.revision.update((n) => n + 1);
            }
        });
    }

    ngOnInit(): void {
        if (this.readOnly()) {
            return;
        }

        // Only what the employee may withdraw, which is narrower than everything HR holds for them. A failure
        // leaves an empty picker and the explanation above it, rather than an error toast on a screen that
        // otherwise works.
        this._lookups.getMyWithdrawableDocuments().subscribe({
            next: (result) => {
                this.documents.set(result ?? []);
                this.documentsLoaded.set(true);
            },
            error: () => {
                this.documents.set([]);
                this.documentsLoaded.set(true);
            },
        });

        // The same active-colleague list the delegation screen uses; the server excludes the caller.
        this._lookups.getDelegationCandidates().subscribe({
            next: (result) => this.guarantorCandidates.set(result ?? []),
            error: () => this.guarantorCandidates.set([]),
        });
    }

    // ------------------------------------------------------------------ what is shown

    isReturn(): boolean {
        return this.value().action === WorkflowRequestAction.Return;
    }

    /** The withdrawal's own fields can be typed into only while a withdrawal is being filled in. */
    editable(): boolean {
        return !this.readOnly() && !this.isReturn();
    }

    /** The document chosen in the picker, for the read-only fields beside it. */
    selectedDocument(): EssWithdrawableDocumentDto | undefined {
        this.revision();

        const id = this.value().employeeDocumentId;

        return id ? this.documents().find((document) => document.id === id) : undefined;
    }

    /** True when the chosen document is already out, so this request would extend its return date. */
    documentIsOut(): boolean {
        const id = this.value().employeeDocumentId;

        return this.editable() && !!id && this.documentsOut().includes(id);
    }

    documentNumber(): string {
        return this.documentDetail((document) => document.documentNum ?? '', this.value().documentNumber);
    }

    issueDate(): string {
        return this.documentDetail(
            (document) => this.formDate(document.issueDate),
            this.asDayMonthYear(this.value().issueDate)
        );
    }

    expiryDate(): string {
        return this.documentDetail(
            (document) => this.formDate(document.expiryDate),
            this.asDayMonthYear(this.value().expiryDate)
        );
    }

    /**
     * A detail of the chosen document: from the picker's own list while one is being chosen, otherwise what
     * came with the request.
     */
    private documentDetail(
        fromList: (document: EssWithdrawableDocumentDto) => string,
        fromValue: string | undefined
    ): string {
        if (!this.editable()) {
            return fromValue ?? '';
        }

        if (!this.value().employeeDocumentId) {
            return '';
        }

        const chosen = this.selectedDocument();

        return chosen ? fromList(chosen) : (fromValue ?? '');
    }

    /**
     * A saved draft's document when the picker's list does not hold it (or has not arrived yet), so the draft
     * still shows what it was saved with rather than an empty picker.
     */
    unlistedDocumentLabel(): string {
        const id = this.value().employeeDocumentId;

        return id && !this.documents().some((document) => document.id === id) ? this.value().documentLabel || '' : '';
    }

    /** The same, for a saved draft's guarantor. */
    unlistedGuarantorLabel(): string {
        const id = this.value().guarantorId;

        return id && !this.guarantorCandidates().some((colleague) => colleague.id === id)
            ? this.value().guarantorLabel || ''
            : '';
    }

    documentLabel(document: EssWithdrawableDocumentDto): string {
        const type = this.pick(document.documentTypeNameEn, document.documentTypeNameAr);

        return document.documentNum ? type + ' - ' + document.documentNum : type;
    }

    colleagueName(colleague: EssColleagueLookupDto): string {
        return `${colleague.code} - ${this.pick(colleague.fullNameEn, colleague.fullNameAr)}`;
    }

    /** Today as the date fields hold it, for the upper bound on a return date: the server refuses a future one. */
    todayIso(): string {
        return DateTime.local().toFormat('yyyy-MM-dd');
    }

    /** A date as the form's own read-only fields show it. Empty rather than "Invalid DateTime" when unset. */
    private formDate(value: DateTime | undefined): string {
        return value && value.year > 1 ? value.toFormat('dd/MM/yyyy') : '';
    }

    /** A stored date shown the way the rest of the system shows one, as text so no time zone can move it. */
    asDayMonthYear(iso: string | undefined): string {
        const [year, month, day] = (iso ?? '').split('-');

        // The year 1 is a date that was never set, not a date.
        return year && month && day && Number(year) > 1 ? `${day.substring(0, 2)}/${month}/${year}` : '';
    }

    /** Bilingual lookup names follow the UI language; the two fields are maintained separately. */
    private pick(en: string | undefined, ar: string | undefined): string {
        return this.isRtl ? ar || en || '' : en || '';
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
