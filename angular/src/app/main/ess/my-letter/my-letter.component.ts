import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    GetMyRequestsInput,
    MyLetterRequestDto,
    MyLetterRequestServiceProxy,
    RequestLetterInput,
    SaveLetterDraftInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize, switchMap } from 'rxjs/operators';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import {
    LetterRequestFormComponent,
    LetterRequestFormValue,
    emptyLetterRequestForm,
} from './letter-request-form.component';

/**
 * Self-service letter requests -- salary certificates, to-whom-it-may-concern letters, and the rest.
 *
 * <p>Laid out like every other self-service request (the exit permit is the model): the list, a Details view
 * in the shared dialog, and the form as its own component (<c>letter-request-form</c>) with Save, Submit and
 * Cancel, so the Details view and an approver are shown the very same form read-only.</p>
 *
 * <p>A section of the one Documents screen (<c>my-document-center</c>), which owns the page heading and the
 * New Request button and opens this form through <c>openForm</c>.</p>
 */
@Component({
    selector: 'ess-letter-requests',
    templateUrl: './my-letter.component.html',
    imports: [
        CommonModule,
        LocalizePipe,
        LuxonFormatPipe,
        EssRequestDetailModalComponent,
        EssListHeadComponent,
        EssListPagerComponent,
        LetterRequestFormComponent,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyLetterComponent extends MyRequestComponentBase<MyLetterRequestDto> implements OnInit {
    private readonly _service = inject(MyLetterRequestServiceProxy);

    /** Hosted beside the other document lists; the host opens a request named in the address. */
    protected override readonly opensRequestFromAddress = false;

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyLetterRequestDto>(this.rows, () => [
        {
            key: 'requestNumber',
            label: 'RequestNumber',
            text: (row) => row.requestNumber,

            // As a number where it is one, so 9 sorts before 10. A draft has no number yet and sorts as the
            // newest; a letter HR recorded was never requested and sorts below everything that was.
            sort: (row) =>
                row.requestNumber
                    ? Number(row.requestNumber) || row.requestNumber
                    : row.isDraft
                      ? Number.MAX_SAFE_INTEGER
                      : 0,
        },
        { key: 'type', label: 'DocumentType', text: (row) => this.typeName(row), filter: 'select' },
        {
            key: 'effectiveDate',
            label: 'EffectiveDate',
            text: (row) => this.detailDate(row.effectiveDate),
            sort: (row) => row.effectiveDate?.toMillis(),
        },
        { key: 'addressedTo', label: 'AddressedTo', text: (row) => this.addressee(row) },
        {
            key: 'status',
            label: 'Status',
            text: (row) => this.statusLabel(row),
            filter: 'select',
        },
    ]);

    /** The form's data, owned here and written into by the form component through ngModel. */
    form: LetterRequestFormValue = emptyLetterRequestForm();

    ngOnInit(): void {
        this.reload();

        // Failure is silent on purpose: the name is decoration, and an unlinked account is already
        // explained by the panel reload() puts up.
        this._essSessionService.getEssContext().subscribe({
            next: (result) => {
                this.context.set(result);

                // A NEW object rather than a mutated one: the form component is OnPush and takes this as an
                // input, so writing into the existing object would leave the name blank on screen.
                this.form = { ...this.form, employeeDisplay: this.employeeDisplay() };
            },
            error: () => undefined,
        });
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyLetterRequestDto[] }> {
        return this._service.getMyLetterRequests(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyLetterRequest(input);
    }

    protected resetForm(): void {
        const form = emptyLetterRequestForm(this.employeeDisplay());

        // The date the letter speaks as of, which is almost always today.
        form.effectiveDate = DateTime.local().toFormat('yyyy-MM-dd');

        this.form = form;
    }

    /**
     * Save: files what is on the form as a draft and goes back to the list. Only the letter type is asked
     * for, because the row cannot be stored without one; the rest is checked at Submit, since the reason to
     * save rather than submit is usually that something is still missing.
     */
    save(): void {
        if (this.saving()) {
            return;
        }

        if (!this.form.documentTypeId) {
            this.message.warn(this.l('DocumentTypeIsRequired'));
            return;
        }

        this.saving.set(true);

        this._service
            .saveMyLetterRequestDraft(this.draftInput())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => this.notifyDraftSaved());
    }

    /**
     * Submit: sends it into the approval chain. A saved draft, or a request an approver sent back, is saved
     * first and then submitted, so what the approver sees is what is on the screen; a new request is created
     * and submitted in the single call that has always done both.
     */
    submit(): void {
        if (this.saving()) {
            return;
        }

        // Refused here as well as by the server, so the employee is told before the round trip.
        if (!this.form.documentTypeId) {
            this.message.warn(this.l('DocumentTypeIsRequired'));
            return;
        }

        if (!this.form.effectiveDate) {
            this.message.warn(this.l('EffectiveDateIsRequired'));
            return;
        }

        // Both addressees are required because both language templates print one; a letter missing the
        // Arabic addressee cannot be issued, and finding that out at print time is far too late.
        if (!this.form.addressToEn?.trim()) {
            this.message.warn(this.l('AddressToEnIsRequired'));
            return;
        }

        if (!this.form.addressToAr?.trim()) {
            this.message.warn(this.l('AddressToArIsRequired'));
            return;
        }

        this.saving.set(true);

        const draftId = this.editingId();

        if (draftId !== undefined) {
            this._service
                .saveMyLetterRequestDraft(this.draftInput())
                .pipe(switchMap(() => this._service.submitMyLetterRequestDraft(new EntityDto({ id: draftId }))))
                .pipe(finalize(() => this.saving.set(false)))
                .subscribe((result) => this.notifySubmitted(result.submittedForApproval));

            return;
        }

        const input = new RequestLetterInput();
        input.documentTypeId = this.form.documentTypeId;
        input.effectiveDate = DateTime.fromISO(this.form.effectiveDate);
        input.addressToEn = this.form.addressToEn.trim();
        input.addressToAr = this.form.addressToAr.trim();
        input.comments = this.form.comments;

        this._service
            .requestLetter(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    /** Reopens a saved draft, or a request an approver sent back, in the same form it was filled in on. */
    continueDraft(row: MyLetterRequestDto): void {
        // Raw id, not an EntityDto: NSwag flattens EntityDto on GET.
        this._service.getMyLetterRequestDraftForEdit(row.id).subscribe((draft) => {
            const form = emptyLetterRequestForm(this.employeeDisplay());

            form.documentTypeId = draft.documentTypeId || null;

            // What the picker shows until its list arrives, and still shows if the list no longer holds it.
            form.documentTypeLabel = this.typeName(draft);

            // A draft is allowed to hold no date at all; the unset date it stores goes back to an empty field.
            form.effectiveDate = this.toInputDate(draft.effectiveDate);
            form.addressToEn = draft.addressToEn ?? '';
            form.addressToAr = draft.addressToAr ?? '';
            form.comments = draft.comments ?? '';

            this.form = form;
            this.editingId.set(row.id);
            this.showForm.set(true);
        });
    }

    private draftInput(): SaveLetterDraftInput {
        const input = new SaveLetterDraftInput();
        input.id = this.editingId();
        input.documentTypeId = this.form.documentTypeId as number;
        input.effectiveDate = this.form.effectiveDate ? DateTime.fromISO(this.form.effectiveDate) : undefined;
        input.addressToEn = this.form.addressToEn?.trim();
        input.addressToAr = this.form.addressToAr?.trim();
        input.comments = this.form.comments;

        return input;
    }

    /** ISO yyyy-MM-dd for a date input; empty for an unset date rather than the year 1. */
    private toInputDate(value: DateTime | undefined): string {
        return value && value.year > 1 ? value.toFormat('yyyy-MM-dd') : '';
    }

    typeName(row: MyLetterRequestDto): string {
        return this.pick(row.documentTypeNameEn, row.documentTypeNameAr);
    }

    addressee(row: MyLetterRequestDto): string {
        return this.pick(row.addressToEn, row.addressToAr);
    }

    protected requestType(): string {
        return 'EmployeeLetterRequest';
    }

    protected detailTitle(): string {
        return this.l('EssMyLetterRequests');
    }

    protected detailFields(row: MyLetterRequestDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('DocumentType'), value: this.typeName(row) },
            { label: this.l('EffectiveDate'), value: this.detailDate(row.effectiveDate) },
            { label: this.l('AddressedTo'), value: this.addressee(row) },
        ];
    }
}
