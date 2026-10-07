import { ChangeDetectionStrategy, Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    AmendMyLeaveInput,
    ApplyForLeaveInput,
    EntityDto,
    EssContextDto,
    EssSessionServiceProxy,
    ExportMyLeavesInput,
    GetMyLeavesInput,
    HalfdayType,
    LeavePaymentType,
    MyLeaveDto,
    MyLeaveHistoryItemDto,
    MyLeaveHistoryOutcome,
    MyLeaveRequestStatus,
    MyLeaveServiceProxy,
    RequestLeaveCancellationInput,
    ResubmitMyLeaveChangeInput,
    ResubmitMyLeaveInput,
    ReturnFromLeaveInput,
    SaveMyLeaveChangeDraftInput,
    SaveMyLeaveDraftInput,
    WithdrawMyLeaveChangeInput,
    WorkflowRequestAction,
} from '@shared/service-proxies/service-proxies';
import { MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize, switchMap } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRelatedRequest, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { ActivatedRoute } from '@angular/router';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { LeaveFormValue, LeaveRequestFormComponent, emptyLeaveForm } from './leave-request-form.component';

/**
 * Self-service leave, laid out like every other self-service request (the exit permit is the model): a list
 * of the employee's leaves, a Details view that opens the request in the shared dialog, and one form with
 * Save, Submit and Cancel.
 *
 * <p>The form is its own component (<c>leave-request-form</c>) so that the Details view and an approver see
 * the very same form, read-only. It serves the four actions legacy's unified leave request had: a new leave,
 * and -- opened from an approved leave's Actions menu -- an amendment, a cancellation or a return.</p>
 *
 * <p>There is one row per leave. Its Request Type and Status are those of the latest request about it: the
 * leave itself, or an amendment, cancellation or return raised against it. Status is always one of the words
 * every self-service request uses -- a granted cancellation reads "Cancel Leave / Approved". Both are worked
 * out from the requests; nothing extra is stored on the leave.</p>
 */
@Component({
    templateUrl: './my-leave.component.html',
    styleUrls: ['./my-leave.component.less'],
    animations: [appModuleAnimation()],
    imports: [
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        EssRequestDetailModalComponent,
        EssListHeadComponent,
        EssListPagerComponent,
        LeaveRequestFormComponent,
        HelpPageDirective,
        TieredMenu,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyLeaveComponent extends AppComponentBase implements OnInit {
    private readonly _myLeaveService = inject(MyLeaveServiceProxy);
    private readonly _essSessionService = inject(EssSessionServiceProxy);
    private readonly _fileDownloadService = inject(FileDownloadService);

    /**
     * A request named in the address -- the Requests screen's "open on its own screen" link puts it there --
     * waiting for the list to arrive so it can be opened. Used once.
     */
    private requestToOpen: number | undefined = Number(inject(ActivatedRoute).snapshot.queryParamMap.get('requestId')) || undefined;

    readonly detail = viewChild<EssRequestDetailModalComponent>('detail');
    private readonly leaveForm = viewChild<LeaveRequestFormComponent>('leaveForm');

    /** Who the signed-in employee is, for the Employee field and the name on the detail dialog. */
    readonly context = signal<EssContextDto | undefined>(undefined);

    readonly loading = signal(false);
    readonly saving = signal(false);
    readonly notLinked = signal(false);
    readonly showForm = signal(false);

    readonly rows = signal<MyLeaveDto[]>([]);

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyLeaveDto>(this.rows, () => [
        {
            key: 'number',
            label: 'RequestNumber',

            // Typing the number of an amendment, cancellation or return finds the leave it is under.
            text: (row) => [row.requestNumber, ...(row.requests ?? []).map((item) => item.requestNumber)].join(' '),

            // Two kinds of leave have no number. A draft not yet sent sorts as the newest, so it stays at the
            // top of the default order, where the thing still waiting on the employee belongs. A leave HR
            // entered was never requested; it sorts below everything that was.
            sort: (row) => (row.requestNumber ? Number(row.requestNumber) || row.requestNumber : row.isDraft ? Number.MAX_SAFE_INTEGER : 0),
        },
        { key: 'leaveType', label: 'LeaveType', text: (row) => this.leaveTypeName(row), filter: 'select' },
        { key: 'requestType', label: 'RequestType', text: (row) => this.requestTypeLabel(row), filter: 'select' },
        { key: 'startDate', label: 'StartDate', text: (row) => this.fmt(row.startDate), sort: (row) => row.startDate?.toMillis() },
        { key: 'endDate', label: 'EndDate', text: (row) => this.fmt(row.endDate), sort: (row) => row.endDate?.toMillis() },
        { key: 'days', label: 'LeaveDays', text: (row) => row.totalLeaveDays },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    /** The form's data, owned here and written into by the form component through ngModel. */
    form: LeaveFormValue = emptyLeaveForm();

    /**
     * The row the form was opened from: a draft or sent-back leave being continued, or the approved leave an
     * amendment, cancellation or return is against. Undefined for a brand new leave.
     */
    readonly editing = signal<MyLeaveDto | undefined>(undefined);

    /** The row menu's entries, rebuilt for whichever row's Actions button was pressed. */
    readonly activeMenuItems = signal<MenuItem[]>([]);

    /** Rows the employee opened or closed by hand; the rest follow <c>isExpanded</c>'s own rule. */
    private readonly expandedByUser = signal<Map<number, boolean>>(new Map());

    ngOnInit(): void {
        this.list.exporter = () => this.exportLeaves();

        // Newest request first, by number, until the employee sorts by something else.
        this.list.sortKey.set('number');
        this.list.sortDescending.set(true);
        this.reload();

        // Failure is silent on purpose: the name is decoration, and an unlinked account is already explained
        // by the panel reload() puts up.
        this._essSessionService.getEssContext().subscribe({
            next: (result) => {
                this.context.set(result);
                this.form = { ...this.form, employeeDisplay: this.employeeDisplay() };
            },
            error: () => undefined,
        });
    }

    reload(): void {
        this.loading.set(true);

        this._myLeaveService
            .getMyLeaves(new GetMyLeavesInput({ includeHistory: true, includeWithdrawn: true, maxResultCount: 200, skipCount: 0, sorting: undefined }))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => {
                    this.rows.set(result.items);
                    this.openRequestFromAddress();
                },

                // An unlinked account is a setup gap, not a fault of theirs -- the page explains it.
                error: () => this.notLinked.set(true),
            });
    }

    /**
     * Opens the request the address names: narrows the list to the leave it belongs to, shows the requests
     * under it, and opens that request's details -- so nobody arriving from the Requests screen has to look
     * for it again.
     */
    private openRequestFromAddress(): void {
        const requestId = this.requestToOpen;
        if (!requestId) {
            return;
        }

        this.requestToOpen = undefined;

        const row = this.rows().find(
            (leave) => leave.workflowRequestId === requestId || (leave.requests ?? []).some((item) => item.workflowRequestId === requestId)
        );

        if (!row) {
            // A leave withdrawn before it was approved is no longer a row; its request is still there to read.
            this.detail()?.show({
                title: this.l('EssMyLeave'),
                requesterName: this.employeeDisplay(),
                workflowRequestId: requestId,
                statusLabel: '',
                statusClass: '',
            });

            return;
        }

        const number =(row.requests ?? []).find((item) => item.workflowRequestId === requestId)?.requestNumber ?? row.requestNumber;
        this.list.setFilter('number', number ?? '');

        if (!this.isExpanded(row)) {
            this.toggleExpanded(row);
        }

        this.openDetail(row, requestId);
    }

    // ------------------------------------------------------------------ opening the form

    /** New Leave: an empty form. */
    openForm(): void {
        this.editing.set(undefined);
        this.form = emptyLeaveForm(WorkflowRequestAction.New, this.employeeDisplay());
        this.show();
    }

    /** Reopens a leave the employee saved, or one an approver sent back, in the form it was filled in on. */
    continueLeave(row: MyLeaveDto): void {
        // Raw id, not an EntityDto: NSwag flattens EntityDto on GET.
        this._myLeaveService.getMyLeaveForEdit(row.id).subscribe((leave) => {
            const form = emptyLeaveForm(WorkflowRequestAction.New, this.employeeDisplay());
            const person = (id: number | undefined, name: string | undefined) => (id ? { id, label: name ?? '' } : null);

            form.leaveId = leave.id;
            form.absenceTypeId = leave.absenceTypeId;
            form.startDate = this.toInputDate(leave.startDate);
            form.endDate = this.toInputDate(leave.endDate);
            form.halfdayType = leave.halfdayType ?? HalfdayType.None;
            form.startTime = form.halfdayType === HalfdayType.None ? leave.startTime ?? '' : '';
            form.endTime = form.halfdayType === HalfdayType.None ? leave.endTime ?? '' : '';
            form.leavePaymentType = leave.leavePaymentType || LeavePaymentType.PayWithPayroll;
            form.paymentDate = this.toInputDate(leave.paymentDate);
            form.isTravelling = !!leave.isTravelling;
            form.primaryDelegate = person(leave.primaryDelegateId, leave.primaryDelegateName);
            form.secondaryDelegate = person(leave.secondaryDelegateId, leave.secondaryDelegateName);
            form.approverDelegate = person(leave.approverDelegateId, leave.approverDelegateName);
            form.policyAccepted = !!leave.policyAccepted;
            form.comments = leave.comments ?? '';

            this.editing.set(row);
            this.form = form;
            this.show();
        });
    }

    /**
     * Opens the form for an amendment, cancellation or return against an approved leave. When the leave
     * already has that request drafted or sent back, the form starts from what was proposed, since that is
     * what is being corrected; otherwise from what the leave says today.
     */
    openChange(row: MyLeaveDto, action: WorkflowRequestAction): void {
        const reopening = !!row.hasPendingAmendment;
        const form = emptyLeaveForm(action, this.employeeDisplay());

        form.leaveId = row.id;
        form.absenceTypeId = (reopening && row.proposedAbsenceTypeId) || row.absenceTypeId;
        form.leaveTypeLabel = this.leaveTypeName(row);
        form.currentLeave = `${this.leaveTypeName(row)} · ${this.fmt(row.startDate)} – ${this.fmt(row.endDate)} · ${row.totalLeaveDays}`;
        form.currentLeaveDays = row.totalLeaveDays;
        form.comments = (reopening && row.pendingReason) || '';

        if (action === WorkflowRequestAction.Amendment) {
            form.startDate = this.toInputDate((reopening && row.proposedStartDate) || row.startDate);
            form.endDate = this.toInputDate((reopening && row.proposedEndDate) || row.endDate);
        } else {
            form.startDate = this.toInputDate(row.startDate);
            form.endDate = this.toInputDate(row.endDate);
        }

        if (action === WorkflowRequestAction.Return) {
            // The day after the leave -- the on-time answer -- so the common case needs no typing.
            form.returnDate = this.toInputDate((reopening && row.proposedReturnDate) || row.endDate?.plus({ days: 1 }));
        }

        this.editing.set(row);
        this.form = form;
        this.show();
    }

    private show(): void {
        this.showForm.set(true);

        // The form sits above the list; without this a button far down the page appears to do nothing.
        setTimeout(() => document.getElementById('leaveFormAnchor')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }

    closeForm(): void {
        this.showForm.set(false);
        this.editing.set(undefined);
        this.leaveForm()?.clearAttachments();
    }

    // ------------------------------------------------------------------ Save and Submit

    /**
     * Save: files what is on the form as a draft and goes back to the list. The leave rules are not applied;
     * that happens at Submit, because the reason to save rather than submit is usually that something is
     * still missing.
     */
    save(): void {
        if (this.saving()) {
            return;
        }

        if (this.form.action !== WorkflowRequestAction.New) {
            this.saving.set(true);

            this._myLeaveService
                .saveMyLeaveChangeDraft(this.changeInput(new SaveMyLeaveChangeDraftInput()))
                .pipe(finalize(() => this.saving.set(false)))
                .subscribe(() => this.done('RequestSavedAsDraft', true));

            return;
        }

        if (!this.form.absenceTypeId || !this.form.startDate || !this.form.endDate) {
            this.message.warn(this.l('LeaveAndDatesNeededToSave'));
            return;
        }

        void this.withAttachments((tokens) => {
            const input = this.leaveInput(new SaveMyLeaveDraftInput(), tokens);
            input.id = this.form.leaveId;

            return this._myLeaveService.saveMyLeaveDraft(input);
        }, 'RequestSavedAsDraft', true);
    }

    /**
     * Submit: sends it into the approval chain. A new leave is created and sent in one call; a saved draft is
     * saved again first, so that what the approver sees is what is on the screen; a request that was sent
     * back goes round again as the same request.
     */
    submit(): void {
        if (this.saving()) {
            return;
        }

        if (this.form.action !== WorkflowRequestAction.New) {
            this.submitChange();
            return;
        }

        const form = this.leaveForm();
        const type = form?.selectedType();

        if (!this.form.absenceTypeId || !this.form.startDate || !this.form.endDate) {
            this.message.warn(this.l('LeaveAndDatesNeededToSave'));
            return;
        }

        if (form?.timesRequired() && (!this.form.startTime || !this.form.endTime)) {
            this.message.warn(this.l('LeaveTypeCanOnlyBeAppliedAsPartialLeave', form.typeNameOf(type)));
            return;
        }

        if (type?.requirePolicyAcceptance && !this.form.policyAccepted) {
            this.message.warn(this.l('PleaseAcceptTheLeavePolicy'));
            return;
        }

        // A leave being continued may already carry its document; the server checks either way.
        if (type?.isAttachmentRequired && !this.form.leaveId && (form?.pendingAttachmentCount() ?? 0) === 0) {
            this.message.warn(this.l('PleaseAttachDocument', form?.typeNameOf(type)));
            return;
        }

        const row = this.editing();

        void this.withAttachments((tokens) => {
            if (row?.isDraft) {
                const draft = this.leaveInput(new SaveMyLeaveDraftInput(), tokens);
                draft.id = row.id;

                return this._myLeaveService
                    .saveMyLeaveDraft(draft)
                    .pipe(switchMap(() => this._myLeaveService.submitMyLeaveDraft(new EntityDto({ id: row.id }))));
            }

            if (row?.isSentBack) {
                const resend = this.leaveInput(new ResubmitMyLeaveInput(), tokens);
                resend.employeeAbsenceId = row.id;

                return this._myLeaveService.resubmitMyLeave(resend);
            }

            return this._myLeaveService.applyForLeave(this.leaveInput(new ApplyForLeaveInput(), tokens));
        }, 'RequestSubmittedForApproval', false);
    }

    private submitChange(): void {
        const row = this.editing();
        if (!row) {
            return;
        }

        const reason = this.form.comments?.trim();
        const action = this.form.action;

        // Required, not merely encouraged: an approver deciding on a change to something they already granted
        // needs to know what prompted it.
        if (action !== WorkflowRequestAction.Return && !reason) {
            this.message.warn(this.l('AReasonIsRequiredForAChange'));
            return;
        }

        // A date box left half-filled -- day and month typed, no year -- holds nothing at all. Sent like that
        // the server can only answer that it could not read a date, which tells the employee nothing.
        if (action === WorkflowRequestAction.Return && !this.form.returnDate) {
            this.message.warn(this.l('EnterTheReturnDateInFull'));
            return;
        }

        if (action === WorkflowRequestAction.Amendment) {
            if (!this.form.startDate || !this.form.endDate) {
                this.message.warn(this.l('EnterBothDatesInFull'));
                return;
            }

            if (this.form.endDate < this.form.startDate) {
                this.message.warn(this.l('EndDateCannotBeEarlierThanStartDate'));
                return;
            }
        }

        let call: Observable<unknown>;

        if (row.hasPendingAmendment) {
            // Drafted earlier, or sent back: the same request is corrected and sent.
            call = this._myLeaveService.resubmitMyLeaveChange(this.changeInput(new ResubmitMyLeaveChangeInput()));
        } else if (action === WorkflowRequestAction.Cancellation) {
            const cancel = new RequestLeaveCancellationInput();
            cancel.id = row.id;
            cancel.reason = reason;
            call = this._myLeaveService.requestCancellationOfMyLeave(cancel);
        } else if (action === WorkflowRequestAction.Return) {
            const returned = new ReturnFromLeaveInput();
            returned.employeeAbsenceId = row.id;
            returned.rejoiningDate = DateTime.fromISO(this.form.returnDate);
            returned.reason = reason;

            // Only a late return carries the paid and unpaid ranges; sent as the employee left them.
            if (this.leaveForm()?.isLateReturn()) {
                returned.paidStartDate = this.toDate(this.form.paidStartDate);
                returned.paidEndDate = this.toDate(this.form.paidEndDate);
                returned.unpaidStartDate = this.toDate(this.form.unpaidStartDate);
                returned.unpaidEndDate = this.toDate(this.form.unpaidEndDate);
            }

            call = this._myLeaveService.returnFromMyLeave(returned);
        } else {
            const amend = new AmendMyLeaveInput();
            amend.employeeAbsenceId = row.id;
            amend.absenceTypeId = this.form.absenceTypeId!;
            amend.startDate = DateTime.fromISO(this.form.startDate);
            amend.endDate = DateTime.fromISO(this.form.endDate);
            amend.reason = reason;
            call = this._myLeaveService.amendMyLeave(amend);
        }

        this.saving.set(true);

        call.pipe(finalize(() => this.saving.set(false))).subscribe(() => this.done('RequestSubmittedForApproval', false));
    }

    /** The form's fields as the server takes them for a new leave. */
    private leaveInput<T extends ApplyForLeaveInput>(input: T, tokens: string[]): T {
        const f = this.form;
        const timesApply = !!f.startDate && f.startDate === f.endDate && f.halfdayType === HalfdayType.None;

        input.absenceTypeId = f.absenceTypeId!;
        input.startDate = DateTime.fromISO(f.startDate);
        input.endDate = DateTime.fromISO(f.endDate);
        input.comments = f.comments;
        input.leavePaymentType = f.leavePaymentType;

        // Sent only for an advance: left empty, the server fills in the day legacy would choose.
        input.paymentDate = f.leavePaymentType === LeavePaymentType.PayInAdvance ? this.toDate(f.paymentDate) : undefined;
        input.halfdayType = f.halfdayType;
        input.startTime = timesApply ? f.startTime || undefined : undefined;
        input.endTime = timesApply ? f.endTime || undefined : undefined;
        input.isTravelling = f.isTravelling;
        input.primaryDelegateId = f.primaryDelegate?.id;
        input.secondaryDelegateId = f.secondaryDelegate?.id;
        input.approverDelegateId = f.approverDelegate?.id;
        input.policyAccepted = f.policyAccepted;
        input.attachmentFileTokens = tokens;

        return input;
    }

    /** The form's fields as the server takes them for an amendment, cancellation or return. */
    private changeInput<T extends ResubmitMyLeaveChangeInput>(input: T): T {
        const f = this.form;

        input.employeeAbsenceId = this.editing()!.id;
        input.reason = f.comments?.trim() || undefined;

        if (f.action === WorkflowRequestAction.Amendment) {
            input.absenceTypeId = f.absenceTypeId ?? undefined;
            input.startDate = this.toDate(f.startDate);
            input.endDate = this.toDate(f.endDate);
        }

        if (f.action === WorkflowRequestAction.Return) {
            input.rejoiningDate = this.toDate(f.returnDate);

            if (this.leaveForm()?.isLateReturn()) {
                input.paidStartDate = this.toDate(f.paidStartDate);
                input.paidEndDate = this.toDate(f.paidEndDate);
                input.unpaidStartDate = this.toDate(f.unpaidStartDate);
                input.unpaidEndDate = this.toDate(f.unpaidEndDate);
            }
        }

        if (input instanceof SaveMyLeaveChangeDraftInput) {
            input.action = f.action;
        }

        return input;
    }

    /**
     * Uploads the files chosen on the form, then makes the call with their tokens. They go WITH the leave: a
     * type that requires a document is refused at submit without one, so it cannot be attached afterwards.
     */
    private async withAttachments(call: (tokens: string[]) => Observable<unknown>, successKey: string, asInfo: boolean): Promise<void> {
        this.saving.set(true);

        try {
            const tokens = (await this.leaveForm()?.uploadAttachments()) ?? [];

            call(tokens)
                .pipe(finalize(() => this.saving.set(false)))
                .subscribe(() => this.done(successKey, asInfo));
        } catch {
            this.saving.set(false);
        }
    }

    private done(successKey: string, asInfo: boolean): void {
        asInfo ? this.notify.info(this.l(successKey)) : this.notify.success(this.l(successKey));
        this.closeForm();
        this.reload();
    }

    // ------------------------------------------------------------------ the list

    /** Opens the row's Actions menu. One menu for the whole table, filled for the row that was pressed. */
    showMenu(event: Event, row: MyLeaveDto, menu: TieredMenu): void {
        this.activeMenuItems.set(this.menuItems(row));
        menu.toggle(event);
    }

    /**
     * Every action, on every row, in the same order: the ones that do not apply to the row are greyed out
     * rather than left out, so the menu always looks the same.
     */
    private menuItems(row: MyLeaveDto): MenuItem[] {
        const pendingWithMe = !!row.hasPendingAmendment && (!!row.pendingIsDraft || !!row.pendingIsSentBack);
        const leaveWithMe = !!row.isDraft || !!row.isSentBack;
        const item = (key: string, enabled: boolean, command: () => void): MenuItem => ({ label: this.l(key), disabled: !enabled, command });

        return [
            item('ContinueDraft', leaveWithMe || pendingWithMe, () =>
                leaveWithMe ? this.continueLeave(row) : this.openChange(row, row.pendingAction ?? WorkflowRequestAction.Amendment)
            ),
            item('AmendLeave', !!row.canAmend, () => this.openChange(row, WorkflowRequestAction.Amendment)),
            item('CancelLeave', !!row.canRequestCancellation, () => this.openChange(row, WorkflowRequestAction.Cancellation)),
            item('ReturnFromLeave', !!row.canReturn, () => this.openChange(row, WorkflowRequestAction.Return)),
            item('Withdraw', !!row.hasPendingAmendment || !!row.canCancel, () =>
                row.hasPendingAmendment ? this.withdrawChange(row) : this.withdrawLeave(row)
            ),
            item('LeaveHistory', !!row.requests?.length, () => this.toggleExpanded(row)),
        ];
    }
    /** Takes back a new leave that has not been decided: a draft, one still with an approver, or one sent back. */
    private withdrawLeave(row: MyLeaveDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l(row.isDraft ? 'DiscardDraft' : 'Withdraw'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._myLeaveService.cancelMyLeave(new EntityDto({ id: row.id })).subscribe(() => {
                this.notify.success(this.l('SuccessfullyDeleted'));
                this.closeForm();
                this.reload();
            });
        });
    }

    /** Takes back an amendment, cancellation or return; the approved leave is exactly as it was. */
    private withdrawChange(row: MyLeaveDto): void {
        this.message.confirm(this.l('WithdrawChangeWarning'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            const input = new WithdrawMyLeaveChangeInput();
            input.id = row.id;

            this._myLeaveService.withdrawMyLeaveChange(input).subscribe(() => {
                this.notify.success(this.l('RequestWithdrawn'));
                this.closeForm();
                this.reload();
            });
        });
    }

    /**
     * Opens the leave read-only in the shared dialog: the form as the leave stands today, the comment, the
     * approvers' remarks, attachments and the action trail -- and, under the form, every request raised against
     * it, each of which opens in the same dialog. Opened from one of the lines under a row, it goes straight to
     * that request.
     */
    openDetail(row: MyLeaveDto, workflowRequestId?: number): void {
        const related: EssRelatedRequest[] = (row.requests ?? []).map((item) => ({
            workflowRequestId: item.workflowRequestId,
            requestNumber: item.requestNumber ?? '',
            typeLabel: this.actionLabel(item.action),
            askedFor: this.askedFor(item),
            statusLabel: this.outcomeLabel(item),
            statusClass: this.outcomeClass(item),
            decidedBy: this.decidedBy(item),
        }));

        // The leave's own request shows the leave as it stands today; a request under it shows what it asked.
        const opened = related.find((item) => item.workflowRequestId === (workflowRequestId ?? row.workflowRequestId));

        this.detail()?.show({
            title: this.l('EssMyLeave'),
            fields: [
                { label: this.l('Employee'), value: this.employeeDisplay() },
                { label: this.l('LeaveType'), value: this.leaveTypeName(row) },
                { label: this.l('StartDate'), value: this.fmt(row.startDate) },
                { label: this.l('EndDate'), value: this.fmt(row.endDate) },
                { label: this.l('LeaveDays'), value: `${row.totalLeaveDays ?? ''}` },
            ],
            comments: row.comments,
            requesterName: this.employeeDisplay(),
            workflowRequestId: opened?.workflowRequestId ?? row.workflowRequestId,
            statusLabel: opened?.statusLabel ?? this.statusLabel(row),
            statusClass: opened?.statusClass ?? this.statusClass(row),
            relatedTitle: this.l('RequestsOnThisLeave'),
            relatedRequests: related,
        });
    }

    /** The row's one status, in the words every self-service request uses. */
    statusLabel(row: MyLeaveDto): string {
        return this.l('WorkflowRequestStatus_' + MyLeaveRequestStatus[row.displayStatus]);
    }

    statusClass(row: MyLeaveDto): string {
        switch (row.displayStatus) {
            case MyLeaveRequestStatus.Open:
                return 'badge-warning';
            case MyLeaveRequestStatus.Approved:
                return 'badge-success';
            case MyLeaveRequestStatus.Rejected:
                return 'badge-danger';
            case MyLeaveRequestStatus.Draft:
            case MyLeaveRequestStatus.SentBack:
                return 'badge-light-warning';
            default:
                return 'badge-secondary';
        }
    }

    /** Which request the row is showing: the leave itself, or the latest one raised against it. */
    requestTypeLabel(row: MyLeaveDto): string {
        return this.actionLabel(row.requestAction);
    }

    // ------------------------------------------------------------------ the requests under a leave

    /**
     * Whether a leave's requests are showing. A leave with something in progress against it opens by itself,
     * so a pending amendment and the dates it proposes cannot be missed; the rest stay closed. Once the
     * employee opens or closes a row, that is what it does.
     */
    isExpanded(row: MyLeaveDto): boolean {
        return this.expandedByUser().get(row.id) ?? !!row.hasPendingAmendment;
    }

    toggleExpanded(row: MyLeaveDto): void {
        const next = new Map(this.expandedByUser());
        next.set(row.id, !this.isExpanded(row));
        this.expandedByUser.set(next);
    }

    actionLabel(action: WorkflowRequestAction | undefined): string {
        switch (action) {
            case WorkflowRequestAction.Amendment:
                return this.l('AmendLeave');
            case WorkflowRequestAction.Cancellation:
                return this.l('CancelLeave');
            case WorkflowRequestAction.Return:
                return this.l('ReturnFromLeave');
            default:
                return this.l('NewLeave');
        }
    }

    /** What one request asked for, in a few words: the dates and days, the day of return, or the cancellation. */
    askedFor(item: MyLeaveHistoryItemDto): string {
        if (item.action === WorkflowRequestAction.Cancellation) {
            return this.l('CancelWholeLeave');
        }

        if (item.action === WorkflowRequestAction.Return) {
            return [this.l('BackOn', this.fmt(item.returnDate)), this.paidUnpaid(item)].filter((part) => !!part).join(' · ');
        }

        const days = item.days === undefined || item.days === null ? '' : `${item.days} ${this.l('LeaveDays')}`;

        return [`${this.fmt(item.startDate)} – ${this.fmt(item.endDate)}`, days].filter((part) => !!part).join(' · ');
    }

    /** For a late return: the extra paid and unpaid ranges it books. Empty otherwise. */
    private paidUnpaid(item: MyLeaveHistoryItemDto): string {
        if (!item.paidStartDate && !item.unpaidStartDate) {
            return '';
        }

        const range = (from?: DateTime, to?: DateTime) => (from && to ? `${this.fmt(from)} – ${this.fmt(to)}` : '–');

        return this.l('LeaveHistory_PaidUnpaid', range(item.paidStartDate, item.paidEndDate), range(item.unpaidStartDate, item.unpaidEndDate));
    }

    outcomeLabel(item: MyLeaveHistoryItemDto): string {
        return this.l('LeaveHistoryOutcome_' + MyLeaveHistoryOutcome[item.outcome]);
    }

    outcomeClass(item: MyLeaveHistoryItemDto): string {
        switch (item.outcome) {
            case MyLeaveHistoryOutcome.AwaitingApproval:
                return 'badge-warning';
            case MyLeaveHistoryOutcome.Approved:
                return 'badge-success';
            case MyLeaveHistoryOutcome.Rejected:
                return 'badge-danger';
            case MyLeaveHistoryOutcome.SentBack:
            case MyLeaveHistoryOutcome.Draft:
                return 'badge-light-warning';
            default:
                return 'badge-secondary';
        }
    }

    /** Who ended the request and when. Empty while it is still with somebody, and for one the employee withdrew. */
    decidedBy(item: MyLeaveHistoryItemDto): string {
        if (!item.decidedOn || item.outcome === MyLeaveHistoryOutcome.Withdrawn) {
            return '';
        }

        const name = this.isRtl ? item.decidedByNameAr || item.decidedByNameEn : item.decidedByNameEn;

        return [name, this.fmt(item.decidedOn)].filter((part) => !!part).join(', ');
    }

    /** Export to Excel: one line per request, built by the server, for the leaves the filters let through. */
    private exportLeaves(): void {
        const input = new ExportMyLeavesInput();
        input.leaveIds = this.list.filtered().map((row) => row.id);

        this._myLeaveService.getMyLeavesToExcel(input).subscribe((file) => this._fileDownloadService.downloadTempFile(file));
    }

    // ------------------------------------------------------------------ small helpers

    fmt(value: DateTime | undefined): string {
        return value ? value.toFormat('dd/MM/yyyy') : '';
    }

    private toDate(value: string): DateTime | undefined {
        return value ? DateTime.fromISO(value) : undefined;
    }

    /** ISO yyyy-MM-dd for a date input; empty for an unset date rather than the year 1. */
    private toInputDate(value: DateTime | undefined | false): string {
        return value && value.year > 1 ? value.toFormat('yyyy-MM-dd') : '';
    }

    leaveTypeName(row: MyLeaveDto): string {
        return this.isRtl ? row.absenceTypeNameAr || row.absenceTypeNameEn : row.absenceTypeNameEn;
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

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
