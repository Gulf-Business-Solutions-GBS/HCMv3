import { signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EntityDto, GetMyRequestsInput, RequestStatus } from '@shared/service-proxies/service-proxies';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';

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
export abstract class MyRequestComponentBase<TDto extends { id: number; workflowStatus?: RequestStatus }> extends AppComponentBase {
    readonly loading = signal(false);
    readonly saving = signal(false);
    readonly notLinked = signal(false);
    readonly showForm = signal(false);

    readonly rows = signal<TDto[]>([]);

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
