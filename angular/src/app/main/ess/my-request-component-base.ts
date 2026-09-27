import { Component, inject, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EntityDto,
    EssContextDto,
    EssSessionServiceProxy,
    GetMyRequestsInput,
    RequestStatus,
} from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { EssRequestDetailField, EssRequestDetailModalComponent } from './shared/ess-request-detail-modal.component';

/**
 * The parts every self-service request screen shares: loading state, the unlinked-employee case, the status
 * badge, and withdrawing.
 *
 * <p>Worth factoring out because these are exactly the details that would otherwise drift between five
 * screens built at the same time. A row with no workflow status has to read as "Recorded" rather than
 * getting a status that implies it went through approval; an unlinked account has to be explained rather
 * than shown an empty table. Both are easy to get subtly different five times over.</p>
 *
 * <p>What stays in each component is what genuinely differs: the form fields and the table columns.</p>
 */
// Decorated purely so the viewChild below is legal: Angular only compiles a query when the class it sits on
// is a component or directive. Nothing is ever declared or imported from here.
//
// A @Component with an empty template, matching AppComponentBase, and NOT a @Directive. Angular refuses a
// directive that inherits a component -- NG0903 -- and AppComponentBase is a @Component. A bare @Directive()
// here compiled perfectly and then threw on every ESS screen the moment one was opened.
@Component({
    template: '',
    standalone: false,
})
export abstract class MyRequestComponentBase<TDto extends { id: number; workflowStatus?: RequestStatus }> extends AppComponentBase {
    readonly loading = signal(false);
    readonly saving = signal(false);
    readonly notLinked = signal(false);
    readonly showForm = signal(false);

    readonly rows = signal<TDto[]>([]);

    protected readonly _essSessionService = inject(EssSessionServiceProxy);

    /**
     * Declared here rather than on each screen so every list opens the same dialog. The query resolves
     * against the subclass's own template, which is where `#detail` lives.
     */
    readonly detail = viewChild<EssRequestDetailModalComponent>('detail');

    /**
     * Who the signed-in employee is. Only so the dialog can say whose request this is -- the server takes
     * the employee from the session, so nothing here is ever sent.
     */
    readonly context = signal<EssContextDto | undefined>(undefined);

    /**
     * The draft the form is currently open on, or undefined when the form is being used to raise something
     * new. It is what tells Submit whether it is sending a draft that already exists or creating and
     * sending in one step -- two different calls, and the form itself looks identical either way.
     */
    readonly editingId = signal<number | undefined>(undefined);

    /** The list call for this transaction type. */
    protected abstract fetch(input: GetMyRequestsInput): Observable<{ items: TDto[] }>;

    /** The withdraw call for this transaction type. */
    protected abstract cancelRequest(input: EntityDto): Observable<void>;

    /** Resets the form model; called when the form is opened. */
    protected abstract resetForm(): void;

    /** The heading the detail dialog opens under -- normally the screen's own title. */
    protected abstract detailTitle(): string;

    /**
     * What one row shows when opened, normally its table columns spelled out.
     *
     * <p>Still worth passing even though the dialog fetches the request's own projection: a row raised while
     * workflow is switched off for its type has no workflow request, so there is nothing to fetch and these
     * are all the dialog can show.</p>
     */
    protected abstract detailFields(row: TDto): EssRequestDetailField[];

    /**
     * The catalog <c>Code</c> for this screen's transaction, which is its entity class name --
     * <c>EmployeeAsset</c>, <c>EmployeeLoan</c> and so on.
     *
     * <p>Abstract rather than optional on purpose. It is what lets the screen tell the employee whether
     * pressing the button sends this to somebody, and a screen that quietly forgot to declare it would go
     * back to guessing -- which is the bug this exists to remove. Better that it does not compile.</p>
     */
    protected abstract requestType(): string;

    /**
     * True when this type routes for approval in this tenant.
     *
     * <p>Read from the ESS context, which already carries the catalog rows behind the home-page tiles, so
     * no extra request. Defaults to false while the context is still loading: the button then reads
     * "Save", and a moment later becomes "Submit" if that is what it is. Guessing "Submit" and downgrading
     * would be the worse way round -- it promises an approver who may not exist.</p>
     */
    routesForApproval(): boolean {
        const type = this.context()?.availableTransactionTypes?.find((t) => t.code === this.requestType());

        return !!type?.isWorkflowEnabled;
    }

    /**
     * What the primary button should say.
     *
     * <p>"Submit" when this genuinely goes to an approver and "Save" when it does not. The same word for
     * both is how an employee ends up waiting on a manager who was never asked, or chasing an approval for
     * something that was already final.</p>
     */
    submitLabel(): string {
        return this.l(this.routesForApproval() ? 'Submit' : 'Save');
    }

    /**
     * Opens a submitted request read-only: its fields, the requester's comment, the approvers' remarks and
     * the action trail.
     *
     * <p>This replaced a Workflow History button that showed the trail alone. Until it existed the requester
     * could see a status badge and nothing else -- not what they had sent, not who was sitting on it.</p>
     */
    openDetail(row: TDto): void {
        const dto = row as TDto & { comments?: string; workflowRequestId?: number };

        this.detail()?.show({
            title: this.detailTitle(),
            fields: this.detailFields(row),
            comments: dto.comments,
            // These are the signed-in employee's own requests, so the first post in the conversation is
            // theirs; naming them saves the dialog guessing from the Created activity.
            requesterName: this.employeeDisplay(),
            workflowRequestId: dto.workflowRequestId,
            statusLabel: this.statusLabel(row),
            statusClass: this.statusClass(row),
        });
    }

    /**
     * Fetches the signed-in employee, for the name on the detail dialog.
     *
     * <p>Failure is swallowed: an account with no employee record is already reported by the list call, and
     * a second error toast for the same setup gap only adds noise.</p>
     */
    protected loadContext(): void {
        this._essSessionService.getEssContext().subscribe({
            next: (result) => this.context.set(result),
            error: () => undefined,
        });
    }

    /** "1042 - Ahmad Al Sayed", or just the name when the code is missing. */
    employeeDisplay(): string {
        const ctx = this.context();
        if (!ctx) {
            return '';
        }

        const name = this.isRtl ? ctx.employeeFullNameAr || ctx.employeeFullNameEn : ctx.employeeFullNameEn;

        return [ctx.employeeCode, name].filter((p) => !!p).join(' - ');
    }

    /** A date as the lists and the dialog both show it. Empty rather than "Invalid DateTime" when unset. */
    protected detailDate(value: { toFormat(format: string): string } | undefined): string {
        return value?.toFormat('yyyy-MM-dd') ?? '';
    }

    reload(): void {
        this.loading.set(true);

        this.fetch(new GetMyRequestsInput({ includeHistory: false, maxResultCount: 50, skipCount: 0, sorting: undefined }))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => this.rows.set(result.items),
                // An unlinked account is a setup gap for HR to close, not a fault of theirs, so the page
                // explains it rather than showing an empty table or an error toast. Admin accounts always
                // land here: they are deliberately never linked to an employee.
                error: () => this.notLinked.set(true),
            });
    }

    openForm(): void {
        this.editingId.set(undefined);
        this.resetForm();
        this.showForm.set(true);
    }

    /**
     * Cancel on the create form. Discards what is on screen and puts the list back -- it does not delete
     * anything, because nothing has been saved. Discarding a draft that HAS been saved is Withdraw, on the
     * row, which is a different action with a confirmation in front of it.
     */
    closeForm(): void {
        this.showForm.set(false);
        this.editingId.set(undefined);
    }

    cancel(row: TDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('Withdraw'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.cancelRequest(new EntityDto({ id: row.id })).subscribe(() => {
                this.notify.success(this.l('SuccessfullyDeleted'));
                this.reload();
            });
        });
    }

    /**
     * A row with no workflow status was entered by HR rather than requested, so it is shown as simply
     * recorded rather than given a badge implying it went through approval.
     */
    statusLabel(row: TDto): string {
        if (row.workflowStatus === undefined || row.workflowStatus === null) {
            return this.l('Recorded');
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[row.workflowStatus]);
    }

    statusClass(row: TDto): string {
        switch (row.workflowStatus) {
            case RequestStatus.Open:
                return 'badge-warning';
            case RequestStatus.Approved:
            case RequestStatus.Closed:
                return 'badge-success';
            case RequestStatus.Rejected:
                return 'badge-danger';
            // Deliberately not the same neutral grey as "Recorded". A draft and a finished record are
            // opposites -- one is waiting on the employee, the other is done -- and giving them the same
            // badge is how a draft goes unnoticed in a list until the trip is over.
            case RequestStatus.Draft:
                return 'badge-light-warning';
            default:
                return 'badge-secondary';
        }
    }

    /** Reports which of the two outcomes happened, rather than a generic "saved". */
    protected notifySubmitted(submittedForApproval: boolean): void {
        this.notify.success(this.l(submittedForApproval ? 'RequestSubmittedForApproval' : 'RequestRecorded'));
        this.closeForm();
        this.reload();
    }

    /**
     * The third outcome, and it has to say so plainly. "Saved" on its own reads as "done", and an employee
     * who believes a draft has gone to their manager will not chase it -- which is the failure a draft
     * feature invites if the message is vague.
     */
    protected notifyDraftSaved(): void {
        this.notify.info(this.l('RequestSavedAsDraft'));
        this.closeForm();
        this.reload();
    }

    protected get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }

    /** Bilingual lookup names follow the UI language; the two fields are maintained separately. */
    protected pick(en: string | undefined, ar: string | undefined): string {
        return this.isRtl ? ar || en || '' : en || '';
    }
}
