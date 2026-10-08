import { ChangeDetectionStrategy, Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    GetMyRequestsInput,
    MyDocumentRequestDto,
    MyDocumentServiceProxy,
    RequestDocumentInput,
    SaveDocumentDraftInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable, firstValueFrom } from 'rxjs';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { DocumentRequestFormComponent, DocumentRequestFormValue, emptyDocumentRequestForm } from './document-request-form.component';

/**
 * Self-service document requests, with the scan attached (legacy DocumentRequest.DocAttachment).
 *
 * <p>Laid out like every other self-service request (the exit permit is the model): the list, a Details view
 * in the shared dialog, and the form as its own component (<c>document-request-form</c>) with Save, Submit and
 * Cancel, so the Details view and an approver are shown the very same form read-only.</p>
 */
@Component({
    templateUrl: './my-document.component.html',
    animations: [appModuleAnimation()],
    imports: [
        CommonModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        EssRequestDetailModalComponent,
        EssListHeadComponent,
        EssListPagerComponent,
        AttachmentsComponent,
        DocumentRequestFormComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyDocumentComponent extends MyRequestComponentBase<MyDocumentRequestDto> implements OnInit {
    private readonly _service = inject(MyDocumentServiceProxy);

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyDocumentRequestDto>(this.rows, () => [
        {
            key: 'requestNumber',
            label: 'RequestNumber',
            text: (row) => row.requestNumber,

            // As a number where it is one, so 9 sorts before 10. A draft has no number yet and sorts as the
            // newest; a document HR recorded was never requested and sorts below everything that was.
            sort: (row) => (row.requestNumber ? Number(row.requestNumber) || row.requestNumber : row.isDraft ? Number.MAX_SAFE_INTEGER : 0),
        },
        { key: 'type', label: 'DocumentType', text: (row) => this.typeName(row), filter: 'select' },
        { key: 'number', label: 'DocumentNumber', text: (row) => row.documentNum },
        { key: 'issueDate', label: 'IssueDate', text: (row) => this.detailDate(row.issueDate), sort: (row) => row.issueDate?.toMillis() },
        { key: 'expiryDate', label: 'ExpiryDate', text: (row) => this.detailDate(row.expiryDate), sort: (row) => row.expiryDate?.toMillis() },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    /** The form component, which holds the files chosen on it until the request exists to attach them to. */
    private readonly documentForm = viewChild<DocumentRequestFormComponent>('documentForm');

    /** The row whose attachments are expanded in the list, if any. */
    readonly expandedId = signal<number | null>(null);

    /** The form's data, owned here and written into by the form component through ngModel. */
    form: DocumentRequestFormValue = emptyDocumentRequestForm();

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

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyDocumentRequestDto[] }> {
        return this._service.getMyDocuments(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyDocument(input);
    }

    protected resetForm(): void {
        // Files chosen for whatever the form was open on before do not belong to a new request.
        this.documentForm()?.clearAttachments();

        this.form = emptyDocumentRequestForm(this.employeeDisplay());
    }

    /**
     * Save: files what is on the form as a draft and goes back to the list. Only the document type is asked
     * for, because the row cannot be stored without one; the rest is checked at Submit, since the reason to
     * save rather than submit is usually that something is still missing.
     *
     * <p>The files chosen on the form are attached to the draft as soon as it exists.</p>
     */
    save(): void {
        if (this.saving()) {
            return;
        }

        if (!this.form.documentTypeId) {
            this.message.warn(this.l('DocumentIsRequired'));
            return;
        }

        void this.run(async () => {
            const result = await firstValueFrom(this._service.saveMyDocumentDraft(this.draftInput()));

            // Attached before the form is closed, which destroys the component holding the files.
            await this.documentForm()?.attachPending(this.editingId() ?? result.transactionId);

            this.notifyDraftSaved();
        });
    }

    /**
     * Submit: sends it into the approval chain. A saved draft, or a request an approver sent back, is saved
     * first and then submitted, so what the approver sees is what is on the screen; a new request is created
     * and submitted in the single call that has always done both. Either way the files chosen on the form are
     * attached to the record by its id.
     */
    submit(): void {
        if (this.saving()) {
            return;
        }

        // Refused here as well as by the server, so the employee is told before the round trip.
        if (!this.form.documentTypeId) {
            this.message.warn(this.l('DocumentIsRequired'));
            return;
        }

        if (!this.form.documentNum?.trim()) {
            this.message.warn(this.l('DocumentNumberIsRequired'));
            return;
        }

        if (!this.form.issueDate) {
            this.message.warn(this.l('IssueDateIsRequired'));
            return;
        }

        if (!this.form.expiryDate) {
            this.message.warn(this.l('ExpiryDateIsRequired'));
            return;
        }

        const draftId = this.editingId();

        if (draftId !== undefined) {
            void this.run(async () => {
                await firstValueFrom(this._service.saveMyDocumentDraft(this.draftInput()));

                // While it is still the employee's own draft, so the approver finds the scan already there.
                await this.documentForm()?.attachPending(draftId);

                const result = await firstValueFrom(this._service.submitMyDocumentDraft(new EntityDto({ id: draftId })));

                this.notifySubmitted(result.submittedForApproval);
            });

            return;
        }

        const input = new RequestDocumentInput();
        input.documentTypeId = this.form.documentTypeId;
        input.documentNum = this.form.documentNum.trim();
        input.issueDate = DateTime.fromISO(this.form.issueDate);
        input.expiryDate = DateTime.fromISO(this.form.expiryDate);
        input.comments = this.form.comments;

        void this.run(async () => {
            const result = await firstValueFrom(this._service.requestDocument(input));

            // The request exists now, so the files can be attached to it. Done before closing the form,
            // which destroys the component holding them.
            await this.documentForm()?.attachPending(result.transactionId);

            this.notifySubmitted(result.submittedForApproval);
        });
    }

    /** Reopens a saved draft, or a request an approver sent back, in the same form it was filled in on. */
    continueDraft(row: MyDocumentRequestDto): void {
        // Raw id, not an EntityDto: NSwag flattens EntityDto on GET.
        this._service.getMyDocumentDraftForEdit(row.id).subscribe((draft) => {
            const form = emptyDocumentRequestForm(this.employeeDisplay());

            // The saved record, so the form lists the files it already has.
            form.id = row.id;
            form.documentTypeId = draft.documentTypeId || null;

            // What the picker shows until its list arrives, and still shows if the list no longer holds it.
            form.documentTypeLabel = this.typeName(draft);
            form.documentNum = draft.documentNum ?? '';

            // A draft is allowed to hold no date at all; the unset date it stores goes back to an empty field.
            form.issueDate = this.toInputDate(draft.issueDate);
            form.expiryDate = this.toInputDate(draft.expiryDate);
            form.comments = draft.comments ?? '';

            // Files chosen for whatever the form was open on before do not belong to this draft.
            this.documentForm()?.clearAttachments();

            this.form = form;
            this.editingId.set(row.id);
            this.showForm.set(true);
        });
    }

    private draftInput(): SaveDocumentDraftInput {
        const input = new SaveDocumentDraftInput();
        input.id = this.editingId();
        input.documentTypeId = this.form.documentTypeId as number;
        input.documentNum = this.form.documentNum?.trim();
        input.issueDate = this.form.issueDate ? DateTime.fromISO(this.form.issueDate) : undefined;
        input.expiryDate = this.form.expiryDate ? DateTime.fromISO(this.form.expiryDate) : undefined;
        input.comments = this.form.comments;

        return input;
    }

    /**
     * Runs one save or submit with the buttons disabled throughout -- the server call and the attaching of
     * the files after it. A refused call has already been explained by the error dialog, and leaves the form
     * open with everything still on it.
     */
    private async run(work: () => Promise<void>): Promise<void> {
        this.saving.set(true);

        try {
            await work();
        } catch {
            // Already reported to the employee.
        } finally {
            this.saving.set(false);
        }
    }

    /** ISO yyyy-MM-dd for a date input; empty for an unset date rather than the year 1. */
    private toInputDate(value: DateTime | undefined): string {
        return value && value.year > 1 ? value.toFormat('yyyy-MM-dd') : '';
    }

    toggleAttachments(row: MyDocumentRequestDto): void {
        this.expandedId.set(this.expandedId() === row.id ? null : row.id);
    }

    typeName(row: MyDocumentRequestDto): string {
        return this.pick(row.documentTypeNameEn, row.documentTypeNameAr);
    }

    protected requestType(): string {
        return 'EmployeeDocument';
    }

    protected detailTitle(): string {
        return this.l('EssMyDocuments');
    }

    protected detailFields(row: MyDocumentRequestDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('DocumentType'), value: this.typeName(row) },
            { label: this.l('DocumentNumber'), value: row.documentNum ?? '' },
            { label: this.l('IssueDate'), value: this.detailDate(row.issueDate) },
            { label: this.l('ExpiryDate'), value: this.detailDate(row.expiryDate) },
        ];
    }
}
