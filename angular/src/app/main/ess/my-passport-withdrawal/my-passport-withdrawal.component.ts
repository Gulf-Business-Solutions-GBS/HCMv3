import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    GetMyRequestsInput,
    MyPassportWithdrawalDto,
    MyPassportWithdrawalServiceProxy,
    RequestPassportWithdrawalInput,
    RequestStatus,
    ReturnDocumentInput,
    SavePassportWithdrawalDraftInput,
    WorkflowRequestAction,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize, switchMap } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import {
    PassportWithdrawalFormComponent,
    PassportWithdrawalFormValue,
    emptyPassportWithdrawalForm,
} from './passport-withdrawal-form.component';

/**
 * Self-service passport (and other document) withdrawals.
 *
 * <p>Two legs, not one. Asking for the document out is a request; handing it back is an amendment against
 * the movement that is already live -- the return date is not known when the request is raised. Both are
 * filled in on the one form component (<c>passport-withdrawal-form</c>), which is also what the Details view
 * and an approver are shown, read-only.</p>
 */
@Component({
    templateUrl: './my-passport-withdrawal.component.html',
    animations: [appModuleAnimation()],
    imports: [
        CommonModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        EssRequestDetailModalComponent,
        EssListHeadComponent,
        EssListPagerComponent,
        PassportWithdrawalFormComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyPassportWithdrawalComponent extends MyRequestComponentBase<MyPassportWithdrawalDto> implements OnInit {
    private readonly _service = inject(MyPassportWithdrawalServiceProxy);

    /** The withdrawal whose return is being reported, if the form is open on one. */
    readonly returningRow = signal<MyPassportWithdrawalDto | null>(null);

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyPassportWithdrawalDto>(this.rows, () => [
        {
            key: 'requestNumber',
            label: 'RequestNumber',
            text: (row) => row.requestNumber,

            // As a number where it is one, so 9 sorts before 10. A draft has no number yet and sorts as the
            // newest; a movement HR recorded was never requested and sorts below everything that was.
            sort: (row) => (row.requestNumber ? Number(row.requestNumber) || row.requestNumber : row.isDraft ? Number.MAX_SAFE_INTEGER : 0),
        },
        { key: 'documentType', label: 'DocumentType', text: (row) => this.typeName(row), filter: 'select' },
        { key: 'documentNumber', label: 'DocumentNumber', text: (row) => row.documentNum },
        { key: 'withdrawDate', label: 'WithdrawDate', text: (row) => this.listDate(row.withdrawDate), sort: (row) => row.withdrawDate?.toMillis() },
        {
            key: 'actualWithdrawDate',
            label: 'HandedOverOn',
            text: (row) => this.listDate(row.actualWithdrawDate),
            sort: (row) => row.actualWithdrawDate?.toMillis(),
        },
        { key: 'returnDate', label: 'ReturnDate', text: (row) => this.listDate(row.returnDate), sort: (row) => row.returnDate?.toMillis() },
        {
            key: 'actualReturnDate',
            label: 'ActualReturnDate',
            text: (row) =>
                this.listDate(row.actualReturnDate) || (this.isOut(row) ? this.l(row.isOverdue ? 'Overdue' : 'StillOut') : ''),
            sort: (row) => row.actualReturnDate?.toMillis(),
        },
        { key: 'guarantor', label: 'Guarantor', text: (row) => this.guarantorName(row) },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    /**
     * A date as the list shows it, or nothing for one that was never set. A draft saved without its dates
     * comes back holding the year 1, which would otherwise print as "0001-01-01".
     */
    listDate(value: DateTime | undefined): string {
        return value && value.year > 1 ? this.detailDate(value) : '';
    }

    /** Whether the document has actually left: the withdrawal is in effect and it has not come back. */
    isOut(row: MyPassportWithdrawalDto): boolean {
        const inEffect =
            row.workflowStatus === undefined ||
            row.workflowStatus === null ||
            row.workflowStatus === RequestStatus.Approved ||
            row.workflowStatus === RequestStatus.Closed;

        return inEffect && !this.listDate(row.actualReturnDate);
    }

    /** The form's data, owned here and written into by the form component through ngModel. */
    form: PassportWithdrawalFormValue = emptyPassportWithdrawalForm();

    /**
     * The documents already with the employee: an approved withdrawal that has not come back. Asking for one
     * of them again is an extension, and the form says so before it is sent.
     */
    readonly documentsOut = computed(() =>
        this.rows()
            .filter((row) => row.workflowStatus === RequestStatus.Approved && !row.actualReturnDate)
            .map((row) => row.employeeDocumentId)
    );

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

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyPassportWithdrawalDto[] }> {
        return this._service.getMyPassportWithdrawals(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyPassportWithdrawal(input);
    }

    protected resetForm(): void {
        const form = emptyPassportWithdrawalForm(WorkflowRequestAction.New, this.employeeDisplay());

        // A lone document is chosen by the form itself, once it knows which documents there are.
        form.withdrawDate = this.todayIso();

        this.form = form;
        this.returningRow.set(null);
    }

    /** Cancel on the form, whichever of the two requests it was open on. */
    override closeForm(): void {
        super.closeForm();
        this.returningRow.set(null);
    }

    /**
     * Save: files what is on the form as a draft and goes back to the list. Only the document is asked for,
     * because the row cannot be stored without one; the dates and the reason are checked at Submit, since
     * the reason to save rather than submit is usually that something is still missing.
     */
    save(): void {
        // A return has no draft; the form does not offer Save for one.
        if (this.saving() || this.form.action === WorkflowRequestAction.Return) {
            return;
        }

        if (!this.form.employeeDocumentId) {
            this.message.warn(this.l('DocumentIsRequired'));
            return;
        }

        this.saving.set(true);

        this._service
            .saveMyPassportWithdrawalDraft(this.draftInput())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => this.notifyDraftSaved());
    }

    /**
     * Submit: sends it into the approval chain. A saved draft, or a request an approver sent back, is saved
     * first and then submitted, so what the approver sees is what is on the screen; a new request is created
     * and submitted in the single call that has always done both. A return goes its own way.
     */
    submit(): void {
        if (this.saving()) {
            return;
        }

        if (this.form.action === WorkflowRequestAction.Return) {
            this.submitReturn();
            return;
        }

        // Refused here as well as by the server, so the employee is told before the round trip.
        if (!this.form.employeeDocumentId) {
            this.message.warn(this.l('DocumentIsRequired'));
            return;
        }

        if (!this.form.withdrawDate) {
            this.message.warn(this.l('WithdrawDateIsRequired'));
            return;
        }

        if (!this.form.returnDate) {
            this.message.warn(this.l('ReturnDateIsRequired'));
            return;
        }

        if (this.form.returnDate < this.form.withdrawDate) {
            this.message.warn(this.l('ReturnDateCannotBeEarlierThanWithdrawDate'));
            return;
        }

        this.saving.set(true);

        const draftId = this.editingId();

        if (draftId !== undefined) {
            this._service
                .saveMyPassportWithdrawalDraft(this.draftInput())
                .pipe(switchMap(() => this._service.submitMyPassportWithdrawalDraft(new EntityDto({ id: draftId }))))
                .pipe(finalize(() => this.saving.set(false)))
                .subscribe((result) => this.notifySubmitted(result.submittedForApproval));

            return;
        }

        const input = new RequestPassportWithdrawalInput();
        input.employeeDocumentId = this.form.employeeDocumentId;
        input.withdrawDate = DateTime.fromISO(this.form.withdrawDate);
        input.returnDate = DateTime.fromISO(this.form.returnDate);

        // Legacy asked the employee to name a guarantor on this form and mailed them when the request was
        // raised. Optional here too, so a tenant that does not use guarantors simply leaves it blank.
        input.guarantorId = this.form.guarantorId ?? undefined;

        // Whether this may be left blank is a tenant setting, so the server decides rather than the screen.
        input.withdrawalReason = this.form.withdrawalReason?.trim();
        input.comments = this.form.comments;

        this._service
            .requestPassportWithdrawal(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    /** Reopens a saved draft, or a request an approver sent back, in the same form it was filled in on. */
    continueDraft(row: MyPassportWithdrawalDto): void {
        // Raw id, not an EntityDto: NSwag flattens EntityDto on GET.
        this._service.getMyPassportWithdrawalDraftForEdit(row.id).subscribe((draft) => {
            const form = emptyPassportWithdrawalForm(WorkflowRequestAction.New, this.employeeDisplay());
            const type = this.typeName(draft);

            form.employeeDocumentId = draft.employeeDocumentId || null;

            // A draft is allowed to hold no date at all; the unset date it stores goes back to an empty field.
            form.withdrawDate = this.toInputDate(draft.withdrawDate);
            form.returnDate = this.toInputDate(draft.returnDate);
            form.guarantorId = draft.guarantorId ?? null;
            form.withdrawalReason = draft.withdrawalReason ?? '';
            form.comments = draft.comments ?? '';

            // What the pickers show until their lists arrive, and still show if the lists no longer hold them.
            form.documentLabel = draft.documentNum ? type + ' - ' + draft.documentNum : type;
            form.documentNumber = draft.documentNum ?? '';
            form.issueDate = this.toInputDate(draft.issueDate);
            form.expiryDate = this.toInputDate(draft.expiryDate);
            form.guarantorLabel = this.guarantorName(draft);

            this.returningRow.set(null);
            this.form = form;
            this.editingId.set(row.id);
            this.showForm.set(true);
        });
    }

    private draftInput(): SavePassportWithdrawalDraftInput {
        const input = new SavePassportWithdrawalDraftInput();
        input.id = this.editingId();
        input.employeeDocumentId = this.form.employeeDocumentId as number;
        input.withdrawDate = this.form.withdrawDate ? DateTime.fromISO(this.form.withdrawDate) : undefined;
        input.returnDate = this.form.returnDate ? DateTime.fromISO(this.form.returnDate) : undefined;
        input.guarantorId = this.form.guarantorId ?? undefined;
        input.withdrawalReason = this.form.withdrawalReason?.trim();
        input.comments = this.form.comments;

        return input;
    }

    /**
     * Opens the same form on an approved withdrawal to report the document handed back: the withdrawal as it
     * stands, read-only, with the day it came back and any remarks to fill in.
     */
    openReturnForm(row: MyPassportWithdrawalDto): void {
        const form = emptyPassportWithdrawalForm(WorkflowRequestAction.Return, this.employeeDisplay());

        form.employeeDocumentId = row.employeeDocumentId;
        form.documentLabel = this.typeName(row);
        form.documentNumber = row.documentNum ?? '';
        form.issueDate = this.toInputDate(row.issueDate);
        form.expiryDate = this.toInputDate(row.expiryDate);
        form.withdrawDate = this.toInputDate(row.withdrawDate);
        form.returnDate = this.toInputDate(row.returnDate);
        form.handedOverOn = this.toInputDate(row.actualWithdrawDate);
        form.guarantorId = row.guarantorId ?? null;
        form.guarantorLabel = this.guarantorName(row);
        form.withdrawalReason = row.withdrawalReason ?? '';
        form.actualReturnDate = this.todayIso();

        this.editingId.set(undefined);
        this.form = form;
        this.returningRow.set(row);
        this.showForm.set(true);
    }

    private submitReturn(): void {
        const row = this.returningRow();

        if (!row) {
            return;
        }

        if (!this.form.actualReturnDate) {
            this.message.warn(this.l('ActualReturnDateIsRequired'));
            return;
        }

        if (this.form.withdrawDate && this.form.actualReturnDate < this.form.withdrawDate) {
            this.message.warn(this.l('ActualReturnDateCannotBeEarlierThanWithdrawDate'));
            return;
        }

        const input = new ReturnDocumentInput();
        input.employeeDocumentMvmtId = row.id;
        input.actualReturnDate = DateTime.fromISO(this.form.actualReturnDate);
        input.returnRemarks = this.form.returnRemarks?.trim() || undefined;

        this.saving.set(true);

        this._service
            .returnMyDocument(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                // Always an approval request, never an immediate stamp: the document is not recorded as back
                // until somebody agrees, so saying "submitted" here is the honest message.
                this.notify.success(this.l('RequestSubmittedForApproval'));
                this.closeForm();
                this.reload();
            });
    }

    /** Today as the date fields hold it. */
    private todayIso(): string {
        return DateTime.local().toFormat('yyyy-MM-dd');
    }

    /** ISO yyyy-MM-dd for a date input; empty for an unset date rather than the year 1. */
    private toInputDate(value: DateTime | undefined): string {
        return value && value.year > 1 ? value.toFormat('yyyy-MM-dd') : '';
    }

    /** "2026-10-12 (Request 1042), ..." -- the due dates this movement had before it was extended. */
    private earlierDueDates(row: MyPassportWithdrawalDto): string {
        return (row.extensions ?? [])
            .map((extension) => {
                const date = this.detailDate(extension.returnDate);

                return extension.requestNumber ? `${date} (${this.l('Request')} ${extension.requestNumber})` : date;
            })
            .join(', ');
    }

    typeName(row: MyPassportWithdrawalDto): string {
        return this.pick(row.documentTypeNameEn, row.documentTypeNameAr);
    }

    guarantorName(row: MyPassportWithdrawalDto): string {
        return this.pick(row.guarantorNameEn, row.guarantorNameAr);
    }

    protected requestType(): string {
        return 'EmployeeDocumentMvmt';
    }

    protected detailTitle(): string {
        return this.l('EssMyPassportWithdrawals');
    }

    protected detailFields(row: MyPassportWithdrawalDto): EssRequestDetailField[] {
        const fields: EssRequestDetailField[] = [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('RequestNumber'), value: row.requestNumber ?? '' },
            { label: this.l('DocumentType'), value: this.typeName(row) },
            { label: this.l('DocumentNumber'), value: row.documentNum ?? '' },
            { label: this.l('WithdrawDate'), value: this.detailDate(row.withdrawDate) },
            { label: this.l('HandedOverOn'), value: this.detailDate(row.actualWithdrawDate) },
            { label: this.l('ReturnDate'), value: this.detailDate(row.returnDate) },
            { label: this.l('ActualReturnDate'), value: this.detailDate(row.actualReturnDate) },
            { label: this.l('ReturnRemarks'), value: row.returnRemarks ?? '' },
            { label: this.l('Guarantor'), value: this.guarantorName(row) },
            { label: this.l('WithdrawalReason'), value: row.withdrawalReason ?? '' },
        ];

        if (row.extensions?.length) {
            fields.push({ label: this.l('EarlierDueDates'), value: this.earlierDueDates(row) });
        }

        return fields;
    }
}
