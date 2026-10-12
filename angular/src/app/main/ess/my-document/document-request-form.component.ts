import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { EssLookupServiceProxy, SimpleLookupDto } from '@shared/service-proxies/service-proxies';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';
import { EssRequestFormMode } from '../my-exit-permit/exit-permit-form.component';

/**
 * The document request as the form holds it. Dates are ISO <c>yyyy-MM-dd</c>, because that is what the date
 * inputs read and write.
 */
export interface DocumentRequestFormValue {
    /** "1042 - Ahmad Al Sayed". Shown, never chosen. */
    employeeDisplay: string;

    /** The saved record: a draft being continued, or a request being read. Its files are attached to it, so they are shown. */
    id?: number;

    documentTypeId: number | null;

    /**
     * The words to show where the picker cannot be used: read-only, or a draft whose document type is no
     * longer in the list.
     */
    documentTypeLabel?: string;

    documentNum: string;
    issueDate: string;
    expiryDate: string;
    comments: string;

    /** Read-only only: the description recorded on the document, when there is one. */
    description?: string;
}

/** An empty form. */
export function emptyDocumentRequestForm(employeeDisplay = ''): DocumentRequestFormValue {
    return {
        employeeDisplay,
        documentTypeId: null,
        documentNum: '',
        issueDate: '',
        expiryDate: '',
        comments: '',
    };
}

/**
 * The document request form -- the SAME form whether an employee is filling it in, looking at what they sent,
 * or an approver is deciding on it, exactly as the exit permit form is. Read-only means disabled, not
 * rearranged: every field keeps its place.
 *
 * <p>The scan is attached to the saved record by its id (legacy DocumentRequest.DocAttachment). A request that
 * does not exist yet holds its files in the browser and the host attaches them once it has saved the request;
 * a draft being continued already exists, so its files are listed and added to at once.</p>
 */
@Component({
    selector: 'document-request-form',
    templateUrl: './document-request-form.component.html',
    imports: [DateInputDirective, CommonModule, FormsModule, LocalizePipe, AttachmentsComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocumentRequestFormComponent extends AppComponentBase implements OnInit {
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly mode = input<EssRequestFormMode>('create');

    /** The form's data. In create mode the host passes its own object and this writes into it through ngModel. */
    readonly value = input.required<DocumentRequestFormValue>();

    /** Disabled while the host is saving, so a double Submit cannot raise two requests. */
    readonly saving = input(false);

    /** Save as draft: files the request without sending it to an approver. */
    readonly saved = output<void>();
    readonly submitted = output<void>();
    readonly cancelled = output<void>();

    readonly readOnly = computed(() => this.mode() !== 'create');

    readonly documentTypes = signal<SimpleLookupDto[]>([]);

    private readonly attachments = viewChild<AttachmentsComponent>('formAttachments');

    ngOnInit(): void {
        if (this.readOnly()) {
            return;
        }

        // Every document type, not only the letter ones -- this screen is "I hold document X", which the
        // letter form's narrower list would wrongly exclude a passport from.
        this._lookups.getDocumentTypes().subscribe({
            next: (result) => this.documentTypes.set(result ?? []),
            error: () => this.documentTypes.set([]),
        });
    }

    documentTypeName(type: SimpleLookupDto): string {
        return this.isRtl ? type.nameAr || type.nameEn || '' : type.nameEn || '';
    }

    /**
     * A saved draft's document type when the picker's list does not hold it (or has not arrived yet), so the
     * draft still shows what it was saved with rather than an empty picker.
     */
    unlistedTypeLabel(): string {
        const id = this.value().documentTypeId;

        return id && !this.documentTypes().some((type) => type.id === id) ? this.value().documentTypeLabel || '' : '';
    }

    /**
     * Attaches the files chosen on the form to the request the host has just saved. Resolves once every file
     * has been tried. Nothing to do for a draft being continued: its files were attached as they were chosen.
     */
    async attachPending(entityId: number): Promise<void> {
        await this.attachments()?.attachPending(entityId);
    }

    /** Drops the files chosen but not yet attached, when the form is handed a different request. */
    clearAttachments(): void {
        this.attachments()?.clearPending();
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
