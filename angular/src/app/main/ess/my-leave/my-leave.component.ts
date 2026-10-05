import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    AmendMyLeaveInput,
    ApplyForLeaveInput,
    AttachmentServiceProxy,
    DeductionUnit,
    EntityDto,
    EssColleagueLookupDto,
    EssContextDto,
    EssLookupServiceProxy,
    EssSessionServiceProxy,
    GetMyLeavesInput,
    HalfdayType,
    LeaveBalancePreviewDto,
    LeaveDurationType,
    LeaveFormSettingsDto,
    LeavePaymentType,
    LeaveReturnKind,
    MyLeaveBalanceDto,
    MyLeaveDto,
    MyLeaveHistoryItemDto,
    MyLeaveHistoryOutcome,
    MyLeaveServiceProxy,
    MyLeaveTypeDto,
    PreviewLeaveBalanceInput,
    RequestLeaveCancellationInput,
    RequestStatus,
    ResubmitMyLeaveChangeInput,
    ResubmitMyLeaveInput,
    ReturnFromLeaveInput,
    ReturnFromLeavePreviewDto,
    SickLeaveDeductionSegmentDto,
    WithdrawMyLeaveChangeInput,
    WorkflowRequestAction,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';
import { EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { AutoCompleteModule } from 'primeng/autocomplete';

/** A colleague as the delegate search boxes show one: the id that is sent, and "1042 - Name". */
interface DelegateOption {
    id: number;
    label: string;
}

/** Which of the four things the one form is doing. */
type LeaveFormMode = 'new' | 'amend' | 'cancel' | 'return';

/**
 * Self-service leave on one screen: balances, the employee's leaves, and ONE form that applies for a new
 * leave or -- opened from an approved leave's row -- amends it, cancels it, or reports the return from it.
 *
 * <p>There is one leave row per leave. Each action against it is a request of its own, and the leave itself
 * does not change until that request is approved; so the list always shows the approved dates, with what a
 * waiting request proposes on a second line, and every leave carries a history of the requests raised
 * against it, including the ones that were rejected or withdrawn.</p>
 */
@Component({
    templateUrl: './my-leave.component.html',
    styleUrls: ['./my-leave.component.less'],
    animations: [appModuleAnimation()],
    imports: [
        DateInputDirective,
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        EssRequestDetailModalComponent,
        AttachmentsComponent,
        HelpPageDirective,
        AutoCompleteModule,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyLeaveComponent extends AppComponentBase implements OnInit {
    private readonly _myLeaveService = inject(MyLeaveServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);
    private readonly _essSessionService = inject(EssSessionServiceProxy);
    private readonly _attachmentService = inject(AttachmentServiceProxy);
    private readonly _download = inject(FileDownloadService);

    readonly detail = viewChild<EssRequestDetailModalComponent>('detail');

    /** Files chosen on the form; uploaded and sent with the leave so a required document is there at submit. */
    private readonly _formAttachments = viewChild<AttachmentsComponent>('formAttachments');

    /** Who the signed-in employee is, for the name on the detail dialog. */
    readonly context = signal<EssContextDto | undefined>(undefined);

    readonly loading = signal(false);
    readonly saving = signal(false);
    readonly notLinked = signal(false);

    readonly balances = signal<MyLeaveBalanceDto[]>([]);
    readonly leaves = signal<MyLeaveDto[]>([]);
    readonly leaveTypes = signal<MyLeaveTypeDto[]>([]);
    readonly colleagues = signal<EssColleagueLookupDto[]>([]);
    readonly settings = signal<LeaveFormSettingsDto | undefined>(undefined);

    /** What the delegate search box currently offers. Shared: only one box is being typed in at a time. */
    readonly delegateSuggestions = signal<DelegateOption[]>([]);

    /** What the form is doing, or undefined when it is closed. */
    readonly mode = signal<LeaveFormMode | undefined>(undefined);

    /** The leave the form is acting on. Undefined for a brand new leave. */
    readonly target = signal<MyLeaveDto | undefined>(undefined);

    /** True when the form is correcting something an approver sent back, rather than raising it afresh. */
    readonly resending = signal(false);

    readonly balancePreview = signal<LeaveBalancePreviewDto | undefined>(undefined);
    readonly sickLeaveSegments = signal<SickLeaveDeductionSegmentDto[]>([]);
    readonly returnPreview = signal<ReturnFromLeavePreviewDto | undefined>(undefined);

    /** Whether leave that is over and done with is listed too. Off by default: the list is for what is live. */
    readonly showPast = signal(false);

    /** The leave whose history is open in the list, and that history. */
    readonly historyOf = signal<number | undefined>(undefined);
    readonly history = signal<MyLeaveHistoryItemDto[]>([]);
    readonly historyLoading = signal(false);

    form = this.emptyForm();

    readonly Status = RequestStatus;
    readonly Action = WorkflowRequestAction;
    readonly Outcome = MyLeaveHistoryOutcome;
    readonly ReturnKind = LeaveReturnKind;
    readonly PaymentType = LeavePaymentType;
    readonly Halfday = HalfdayType;
    readonly DeductionUnitPercentage = DeductionUnit.Percentage;

    /** The leave type chosen on the form, with the settings that decide which fields it shows. */
    readonly selectedType = computed(() => {
        this.formVersion();
        return this.leaveTypes().find((t) => t.id === this.form.absenceTypeId);
    });

    /**
     * Bumped whenever a plain form field that a computed depends on changes. The form is a plain object so
     * that ngModel can bind to it; this is what lets signals notice.
     */
    private readonly formVersion = signal(0);

    private emptyForm() {
        return {
            absenceTypeId: null as number | null,
            startDate: '',
            endDate: '',
            startTime: '',
            endTime: '',
            halfdayType: HalfdayType.None,
            comments: '',
            reason: '',
            leavePaymentType: LeavePaymentType.PayWithPayroll,
            paymentDate: '',
            isTravelling: false,
            primaryDelegate: null as DelegateOption | null,
            secondaryDelegate: null as DelegateOption | null,
            approverDelegate: null as DelegateOption | null,
            policyAccepted: false,
            rejoiningDate: '',
            paidStartDate: '',
            paidEndDate: '',
            unpaidStartDate: '',
            unpaidEndDate: '',
        };
    }

    ngOnInit(): void {
        this.loadContext();
        this.loadLookups();
        this.reload();
    }

    reload(): void {
        this.loading.set(true);

        this._myLeaveService.getMyBalances().subscribe({
            next: (result) => this.balances.set(result),
            // An unlinked account is a setup gap, not a fault of theirs -- the page explains it rather than
            // showing an empty list or an error toast.
            error: () => this.notLinked.set(true),
        });

        this._myLeaveService
            .getMyLeaves(new GetMyLeavesInput({ includeHistory: this.showPast(), maxResultCount: 50, skipCount: 0, sorting: undefined }))
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (result) => {
                    this.leaves.set(result.items);

                    // An open history is refreshed with the list, so it shows what was just done.
                    const open = this.historyOf();
                    if (open !== undefined) {
                        this.loadHistory(open);
                    }
                },
                error: () => this.notLinked.set(true),
            });
    }

    togglePast(value: boolean): void {
        this.showPast.set(value);
        this.reload();
    }

    private loadLookups(): void {
        // Already filtered by the server to what THIS employee may apply for: gender, religion, salary profile.
        this._myLeaveService.getMyLeaveTypes().subscribe({
            next: (result) => this.leaveTypes.set(result ?? []),
            error: () => this.leaveTypes.set([]),
        });

        this._myLeaveService.getLeaveFormSettings().subscribe({
            next: (result) => this.settings.set(result),
            error: () => undefined,
        });

        this._lookups.getDelegationCandidates().subscribe({
            next: (result) => this.colleagues.set(result ?? []),
            error: () => this.colleagues.set([]),
        });
    }

    // ------------------------------------------------------------------ opening the form

    openNew(): void {
        this.form = this.emptyForm();
        this.form.leavePaymentType = this.settings()?.defaultLeavePaymentType || LeavePaymentType.PayWithPayroll;
        this.open('new', undefined, false);
    }

    /** A new leave an approver sent back: the same form, filled with what was sent, to correct and resend. */
    openResendNew(leave: MyLeaveDto): void {
        this.form = this.emptyForm();
        this.form.absenceTypeId = leave.absenceTypeId;
        this.form.startDate = leave.startDate?.toISODate() ?? '';
        this.form.endDate = leave.endDate?.toISODate() ?? '';
        this.form.startTime = leave.halfdayType === HalfdayType.None ? leave.startTime ?? '' : '';
        this.form.endTime = leave.halfdayType === HalfdayType.None ? leave.endTime ?? '' : '';
        this.form.halfdayType = leave.halfdayType ?? HalfdayType.None;
        this.form.comments = leave.comments ?? '';

        // Already accepted when it was first sent; the server keeps that stamp.
        this.form.policyAccepted = true;
        this.open('new', leave, true);
    }

    openAmend(leave: MyLeaveDto, resend = false): void {
        this.form = this.emptyForm();
        this.form.absenceTypeId = leave.absenceTypeId;

        // Resending starts from what was PROPOSED, since that is what is being corrected; a fresh amendment
        // starts from what the leave says today.
        this.form.startDate = ((resend && leave.proposedStartDate) || leave.startDate)?.toISODate() ?? '';
        this.form.endDate = ((resend && leave.proposedEndDate) || leave.endDate)?.toISODate() ?? '';
        this.open('amend', leave, resend);
    }

    openCancel(leave: MyLeaveDto, resend = false): void {
        this.form = this.emptyForm();
        this.open('cancel', leave, resend);
    }

    /** Pre-filled with the day after the leave -- the on-time answer -- so the common case is one click. */
    openReturn(leave: MyLeaveDto, resend = false): void {
        this.form = this.emptyForm();
        this.form.rejoiningDate =
            ((resend && leave.proposedReturnDate) || leave.endDate?.plus({ days: 1 }))?.toISODate() ?? '';
        this.open('return', leave, resend);
        this.previewReturn();
    }

    /** Opens whichever form the sent-back request belongs to. */
    openResendChange(leave: MyLeaveDto): void {
        switch (leave.pendingAction) {
            case WorkflowRequestAction.Cancellation:
                this.openCancel(leave, true);
                break;
            case WorkflowRequestAction.Return:
                this.openReturn(leave, true);
                break;
            default:
                this.openAmend(leave, true);
        }
    }

    private open(mode: LeaveFormMode, target: MyLeaveDto | undefined, resending: boolean): void {
        this.target.set(target);
        this.resending.set(resending);
        this.balancePreview.set(undefined);
        this.sickLeaveSegments.set([]);
        this.returnPreview.set(undefined);
        this.mode.set(mode);
        this.formVersion.update((v) => v + 1);
        this.onLeaveDetailsChanged();

        // The form sits above the list; without this the employee presses a button far down the page and
        // sees nothing happen.
        setTimeout(() => document.getElementById('leaveForm')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }

    closeForm(): void {
        this.mode.set(undefined);
        this.target.set(undefined);
        this.resending.set(false);
        this.balancePreview.set(undefined);
        this.sickLeaveSegments.set([]);
        this.returnPreview.set(undefined);
    }

    formTitle(): string {
        switch (this.mode()) {
            case 'amend':
                return this.l('AmendLeave');
            case 'cancel':
                return this.l('CancelLeave');
            case 'return':
                return this.l('ReturnFromLeave');
            default:
                return this.l('NewLeave');
        }
    }

    // ------------------------------------------------------------------ what the form shows

    /** Times are offered for a one-day leave of a type that allows partial leave, unless a half day is chosen. */
    showTimes(): boolean {
        const type = this.selectedType();
        return this.mode() === 'new' && !!type?.allowPartialLeave;
    }

    timesEnabled(): boolean {
        return (
            !!this.form.startDate &&
            this.form.startDate === this.form.endDate &&
            this.form.halfdayType === HalfdayType.None
        );
    }

    timesRequired(): boolean {
        return this.selectedType()?.leaveDurationType === LeaveDurationType.Partial;
    }

    showHalfDay(): boolean {
        return this.showTimes() && !!this.settings()?.enableAutoHalfDayLeave;
    }

    colleagueName(colleague: EssColleagueLookupDto): string {
        const name = this.isRtl ? colleague.fullNameAr || colleague.fullNameEn : colleague.fullNameEn;
        return [colleague.code, name].filter((p) => !!p).join(' - ');
    }

    /**
     * Type-to-search over the colleague list, by staff number or name. A company has hundreds of colleagues,
     * which a plain drop-down cannot usefully show; legacy's picker searched by name and offered the first
     * twenty, and this does the same. The employee already chosen as primary delegate is left out of the
     * secondary delegate's suggestions.
     */
    searchDelegates(event: { query: string }, forSecondary = false): void {
        const query = (event.query ?? '').trim().toLowerCase();
        const excludedId = forSecondary ? this.form.primaryDelegate?.id : undefined;

        const matches: DelegateOption[] = [];
        for (const colleague of this.colleagues()) {
            if (colleague.id === excludedId) {
                continue;
            }

            const label = this.colleagueName(colleague);
            if (!query || label.toLowerCase().includes(query)) {
                matches.push({ id: colleague.id, label });
                if (matches.length === 20) {
                    break;
                }
            }
        }

        this.delegateSuggestions.set(matches);
    }

    onPrimaryDelegateChanged(): void {
        // Legacy: a secondary delegate cannot stand without a primary one, nor be the same person.
        if (!this.form.primaryDelegate || this.form.secondaryDelegate?.id === this.form.primaryDelegate.id) {
            this.form.secondaryDelegate = null;
        }

        this.formVersion.update((v) => v + 1);
    }

    onDatesChanged(): void {
        // Times belong to a one-day leave; a range clears them, as legacy did.
        if (this.form.startDate !== this.form.endDate) {
            this.form.startTime = '';
            this.form.endTime = '';
            this.form.halfdayType = HalfdayType.None;
        }

        this.onLeaveDetailsChanged();
    }

    onHalfdayChanged(): void {
        if (this.form.halfdayType !== HalfdayType.None) {
            this.form.startTime = '';
            this.form.endTime = '';
        }

        this.onLeaveDetailsChanged();
    }

    /**
     * Re-asks the server about this leave whenever the type, dates or times change: the three balance figures
     * and, for sick leave, how it would be paid.
     *
     * <p>The server decides, not the screen: what counts as a day off, how much is already booked and the
     * sick-pay slabs are all rules it owns. A copy of them here would be a second answer to the same
     * question, and the server's would win at submit.</p>
     */
    onLeaveDetailsChanged(): void {
        this.formVersion.update((v) => v + 1);

        const mode = this.mode();
        if ((mode !== 'new' && mode !== 'amend') || !this.form.absenceTypeId) {
            this.balancePreview.set(undefined);
            this.sickLeaveSegments.set([]);
            return;
        }

        const input = new PreviewLeaveBalanceInput();
        input.absenceTypeId = this.form.absenceTypeId;
        input.startDate = this.toDate(this.form.startDate) as DateTime;
        input.endDate = this.toDate(this.form.endDate) as DateTime;
        input.startTime = this.timesEnabled() ? this.form.startTime || undefined : undefined;
        input.endTime = this.timesEnabled() ? this.form.endTime || undefined : undefined;

        // A change is charged only for the difference from what is already approved.
        input.employeeAbsenceId = mode === 'amend' ? this.target()?.id : undefined;

        this._myLeaveService.previewMyLeaveBalance(input).subscribe({
            next: (preview) => this.balancePreview.set(preview),
            error: () => this.balancePreview.set(undefined),
        });

        if (mode !== 'new' || !this.form.startDate || !this.form.endDate) {
            this.sickLeaveSegments.set([]);
            return;
        }

        const sick = new ApplyForLeaveInput();
        sick.absenceTypeId = this.form.absenceTypeId;
        sick.startDate = DateTime.fromISO(this.form.startDate);
        sick.endDate = DateTime.fromISO(this.form.endDate);

        this._myLeaveService.previewMySickLeaveDeduction(sick).subscribe({
            next: (segments) => this.sickLeaveSegments.set(segments ?? []),

            // A preview that cannot be fetched must not stop somebody applying -- it is an explanation of the
            // request, not part of it.
            error: () => this.sickLeaveSegments.set([]),
        });
    }

    /** Opens the leave type's policy document -- the file the acceptance tick refers to. */
    openPolicy(): void {
        const id = this.selectedType()?.policyAttachmentId;
        if (!id) {
            return;
        }

        this._attachmentService.getAttachmentDownload(id).subscribe((file) => this._download.downloadTempFile(file));
    }

    /**
     * Asks the server what the date means rather than working it out here: a gap that is only a weekend counts
     * as on time, and only the server knows the employee's week and holidays. For a late return it also brings
     * the proposed paid/unpaid split, which the employee may then adjust.
     */
    previewReturn(): void {
        const target = this.target();
        if (!target || !this.form.rejoiningDate) {
            this.returnPreview.set(undefined);
            return;
        }

        const input = new ReturnFromLeaveInput();
        input.employeeAbsenceId = target.id;
        input.rejoiningDate = DateTime.fromISO(this.form.rejoiningDate);

        this._myLeaveService.previewReturnFromMyLeave(input).subscribe({
            next: (preview) => {
                this.returnPreview.set(preview);
                this.form.paidStartDate = preview.paidStartDate?.toISODate() ?? '';
                this.form.paidEndDate = preview.paidEndDate?.toISODate() ?? '';
                this.form.unpaidStartDate = preview.unpaidStartDate?.toISODate() ?? '';
                this.form.unpaidEndDate = preview.unpaidEndDate?.toISODate() ?? '';
            },
            error: () => this.returnPreview.set(undefined),
        });
    }

    returnKindMessage(preview: ReturnFromLeavePreviewDto): string {
        switch (preview.kind) {
            case LeaveReturnKind.Early:
                return this.l('LeaveReturnKind_Early', preview.newEndDate?.toFormat('dd/MM/yyyy'));
            case LeaveReturnKind.Late:
                return this.l('LeaveReturnKind_Late', preview.daysDifference);
            default:
                return this.l('LeaveReturnKind_OnTime');
        }
    }

    /**
     * "Submit" when this genuinely goes to an approver and "Save" when it does not. Amend, cancel and return
     * exist only with workflow on, so they always go for approval.
     */
    submitLabel(): string {
        if (this.mode() !== 'new') {
            return this.l('SendForApproval');
        }

        const type = this.context()?.availableTransactionTypes?.find((t) => t.code === 'EmployeeAbsence');

        return this.l(type?.isWorkflowEnabled ? 'SendForApproval' : 'Save');
    }

    // ------------------------------------------------------------------ sending

    submit(): void {
        if (this.saving()) {
            return;
        }

        switch (this.mode()) {
            case 'new':
                void this.submitNew();
                break;
            case 'amend':
                this.submitAmend();
                break;
            case 'cancel':
                this.submitCancel();
                break;
            case 'return':
                this.submitReturn();
                break;
        }
    }

    private async submitNew(): Promise<void> {
        const type = this.selectedType();

        if (!this.form.absenceTypeId || !this.form.startDate || !this.form.endDate) {
            return;
        }

        if (this.timesRequired() && (!this.form.startTime || !this.form.endTime)) {
            this.message.warn(this.l('LeaveTypeCanOnlyBeAppliedAsPartialLeave', this.typeNameOf(type)));
            return;
        }

        if (type?.requirePolicyAcceptance && !this.form.policyAccepted) {
            this.message.warn(this.l('PleaseAcceptTheLeavePolicy'));
            return;
        }

        const attachments = this._formAttachments();
        const resendTarget = this.resending() ? this.target() : undefined;

        // A leave being resent may already carry its document from the first time round; the server checks.
        if (type?.isAttachmentRequired && !resendTarget && (attachments?.pending().length ?? 0) === 0) {
            this.message.warn(this.l('PleaseAttachDocument', this.typeNameOf(type)));
            return;
        }

        this.saving.set(true);

        try {
            // Uploaded first and sent WITH the leave: a type that requires a document is refused at submit
            // without one, so it cannot be attached afterwards as on the other self-service screens.
            const tokens = (await attachments?.uploadPendingToTokens()) ?? [];

            const input = resendTarget ? new ResubmitMyLeaveInput() : new ApplyForLeaveInput();
            input.absenceTypeId = this.form.absenceTypeId;
            input.startDate = DateTime.fromISO(this.form.startDate);
            input.endDate = DateTime.fromISO(this.form.endDate);
            input.comments = this.form.comments;
            input.leavePaymentType = this.form.leavePaymentType;

            // Sent only for an advance: the server fills in the day legacy would choose when it is left empty,
            // and moves it to the first working day when the leave is approved.
            input.paymentDate = this.toDate(this.form.paymentDate);
            input.halfdayType = this.showHalfDay() ? this.form.halfdayType : HalfdayType.None;
            input.startTime = this.showTimes() && this.timesEnabled() ? this.form.startTime || undefined : undefined;
            input.endTime = this.showTimes() && this.timesEnabled() ? this.form.endTime || undefined : undefined;
            input.isTravelling = this.form.isTravelling;
            input.primaryDelegateId = this.form.primaryDelegate?.id;
            input.secondaryDelegateId = this.form.secondaryDelegate?.id;
            input.approverDelegateId = this.form.approverDelegate?.id;
            input.policyAccepted = this.form.policyAccepted;
            input.attachmentFileTokens = tokens;

            if (resendTarget) {
                (input as ResubmitMyLeaveInput).employeeAbsenceId = resendTarget.id;
            }

            const call = resendTarget
                ? this._myLeaveService.resubmitMyLeave(input as ResubmitMyLeaveInput)
                : this._myLeaveService.applyForLeave(input);

            call.pipe(finalize(() => this.saving.set(false))).subscribe((result) => {
                // The outcomes are genuinely different, so say which happened rather than a generic "saved"
                // that implies an approval which may never come.
                this.notify.success(
                    this.l(
                        resendTarget
                            ? 'ResentForApproval'
                            : result.submittedForApproval
                              ? 'LeaveSubmittedForApproval'
                              : 'LeaveRecorded'
                    )
                );

                attachments?.clearPending();
                this.closeForm();
                this.reload();
            });
        } catch {
            this.saving.set(false);
        }
    }

    private submitAmend(): void {
        const target = this.target();
        if (!target || !this.form.absenceTypeId || !this.form.startDate || !this.form.endDate) {
            return;
        }

        // Required, not merely encouraged. An approver deciding on a change to something they already
        // granted needs to know what prompted it.
        if (!this.resending() && !this.form.reason?.trim()) {
            this.message.warn(this.l('AReasonIsRequiredForAChange'));
            return;
        }

        if (this.resending()) {
            const input = new ResubmitMyLeaveChangeInput();
            input.employeeAbsenceId = target.id;
            input.absenceTypeId = this.form.absenceTypeId;
            input.startDate = DateTime.fromISO(this.form.startDate);
            input.endDate = DateTime.fromISO(this.form.endDate);
            input.reason = this.form.reason?.trim() || undefined;
            this.resend(input);
            return;
        }

        const amend = new AmendMyLeaveInput();
        amend.employeeAbsenceId = target.id;
        amend.absenceTypeId = this.form.absenceTypeId;
        amend.startDate = DateTime.fromISO(this.form.startDate);
        amend.endDate = DateTime.fromISO(this.form.endDate);
        amend.reason = this.form.reason.trim();

        this.send(this._myLeaveService.amendMyLeave(amend), 'AmendmentSubmittedForApproval');
    }

    private submitCancel(): void {
        const target = this.target();
        if (!target) {
            return;
        }

        if (!this.resending() && !this.form.reason?.trim()) {
            this.message.warn(this.l('AReasonIsRequiredForAChange'));
            return;
        }

        if (this.resending()) {
            const input = new ResubmitMyLeaveChangeInput();
            input.employeeAbsenceId = target.id;
            input.reason = this.form.reason?.trim() || undefined;
            this.resend(input);
            return;
        }

        const cancel = new RequestLeaveCancellationInput();
        cancel.id = target.id;
        cancel.reason = this.form.reason.trim();

        this.send(this._myLeaveService.requestCancellationOfMyLeave(cancel), 'CancellationSubmittedForApproval');
    }

    private submitReturn(): void {
        const target = this.target();
        if (!target || !this.form.rejoiningDate) {
            return;
        }

        const late = this.returnPreview()?.kind === LeaveReturnKind.Late;

        if (this.resending()) {
            const input = new ResubmitMyLeaveChangeInput();
            input.employeeAbsenceId = target.id;
            input.rejoiningDate = DateTime.fromISO(this.form.rejoiningDate);
            input.reason = this.form.reason?.trim() || undefined;
            if (late) {
                input.paidStartDate = this.toDate(this.form.paidStartDate);
                input.paidEndDate = this.toDate(this.form.paidEndDate);
                input.unpaidStartDate = this.toDate(this.form.unpaidStartDate);
                input.unpaidEndDate = this.toDate(this.form.unpaidEndDate);
            }
            this.resend(input);
            return;
        }

        const input = new ReturnFromLeaveInput();
        input.employeeAbsenceId = target.id;
        input.rejoiningDate = DateTime.fromISO(this.form.rejoiningDate);
        input.reason = this.form.reason?.trim();

        // Only a late return carries a split; sent as the employee left it, and checked by the server.
        if (late) {
            input.paidStartDate = this.toDate(this.form.paidStartDate);
            input.paidEndDate = this.toDate(this.form.paidEndDate);
            input.unpaidStartDate = this.toDate(this.form.unpaidStartDate);
            input.unpaidEndDate = this.toDate(this.form.unpaidEndDate);
        }

        this.send(this._myLeaveService.returnFromMyLeave(input), 'ReturnSubmittedForApproval');
    }

    private resend(input: ResubmitMyLeaveChangeInput): void {
        this.send(this._myLeaveService.resubmitMyLeaveChange(input), 'ResentForApproval');
    }

    private send(call: Observable<unknown>, successKey: string): void {
        this.saving.set(true);

        call.pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.success(this.l(successKey));
                this.closeForm();
                this.reload();
            });
    }

    // ------------------------------------------------------------------ withdrawing

    /** Takes back a new leave that has not been decided: still with an approver, or sent back. */
    withdrawLeave(leave: MyLeaveDto): void {
        this.message.confirm(this.l('LeaveCancelWarningMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this._myLeaveService.cancelMyLeave(new EntityDto({ id: leave.id })).subscribe(() => {
                this.notify.success(this.l('RequestWithdrawn'));
                this.closeForm();
                this.reload();
            });
        });
    }

    /** Takes back an amendment, cancellation or return; the approved leave is exactly as it was. */
    withdrawChange(leave: MyLeaveDto): void {
        this.message.confirm(this.l('WithdrawChangeWarning'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            const input = new WithdrawMyLeaveChangeInput();
            input.id = leave.id;

            this._myLeaveService.withdrawMyLeaveChange(input).subscribe(() => {
                this.notify.success(this.l('RequestWithdrawn'));
                this.closeForm();
                this.reload();
            });
        });
    }

    // ------------------------------------------------------------------ the list

    /** "Amendment pending", "Cancellation pending" or "Return pending" -- or "Returned to you". */
    pendingLabel(leave: MyLeaveDto): string {
        if (leave.pendingIsSentBack) {
            return this.l('ReturnedToYou');
        }

        return this.l(this.actionWord(leave.pendingAction) + 'Pending');
    }

    rejectedLabel(leave: MyLeaveDto): string {
        return this.l(this.actionWord(leave.lastRejectedAction) + 'Rejected');
    }

    openChangeLabel(leave: MyLeaveDto): string {
        return this.l('Open' + this.actionWord(leave.pendingAction));
    }

    withdrawChangeLabel(leave: MyLeaveDto): string {
        return this.l('Withdraw' + this.actionWord(leave.pendingAction));
    }

    private actionWord(action: WorkflowRequestAction | undefined): string {
        switch (action) {
            case WorkflowRequestAction.Cancellation:
                return 'Cancellation';
            case WorkflowRequestAction.Return:
                return 'Return';
            default:
                return 'Amendment';
        }
    }

    /** The second line under the approved dates: what a waiting request would make of the leave. */
    proposedLine(leave: MyLeaveDto): string {
        if (!leave.hasPendingAmendment) {
            return '';
        }

        if (leave.pendingAction === WorkflowRequestAction.Return && leave.proposedReturnDate) {
            return this.l('ProposedReturnOn', this.fmt(leave.proposedReturnDate));
        }

        if (leave.pendingAction === WorkflowRequestAction.Amendment && leave.proposedStartDate && leave.proposedEndDate) {
            return this.l('ProposedDates', this.fmt(leave.proposedStartDate), this.fmt(leave.proposedEndDate));
        }

        return '';
    }

    /**
     * A row with no workflow status was entered by HR rather than requested, so it is shown as simply
     * recorded rather than given a status badge that would imply it went through approval.
     */
    statusLabel(leave: MyLeaveDto): string {
        if (leave.workflowStatus === undefined || leave.workflowStatus === null) {
            return this.l('Recorded');
        }

        if (leave.isSentBack) {
            return this.l('ReturnedToYou');
        }

        return this.l('WorkflowRequestStatus_' + RequestStatus[leave.workflowStatus]);
    }

    statusClass(leave: MyLeaveDto): string {
        switch (leave.workflowStatus) {
            case RequestStatus.Open:
                return 'badge-warning';
            case RequestStatus.Approved:
            case RequestStatus.Closed:
                return 'badge-success';
            case RequestStatus.Rejected:
            case RequestStatus.Cancelled:
                return 'badge-danger';
            case RequestStatus.Draft:
                return 'badge-info';
            default:
                return 'badge-secondary';
        }
    }

    isCancelled(leave: MyLeaveDto): boolean {
        return leave.workflowStatus === RequestStatus.Cancelled;
    }

    // ------------------------------------------------------------------ history

    toggleHistory(leave: MyLeaveDto): void {
        if (this.historyOf() === leave.id) {
            this.historyOf.set(undefined);
            this.history.set([]);
            return;
        }

        this.historyOf.set(leave.id);
        this.history.set([]);
        this.loadHistory(leave.id);
    }

    private loadHistory(leaveId: number): void {
        this.historyLoading.set(true);

        this._myLeaveService
            .getMyLeaveHistory(leaveId)
            .pipe(finalize(() => this.historyLoading.set(false)))
            .subscribe({
                next: (items) => this.history.set(items ?? []),
                error: () => this.history.set([]),
            });
    }

    /** One line saying what the request asked for: the leave, an amendment, a cancellation or a return. */
    historyTitle(item: MyLeaveHistoryItemDto): string {
        switch (item.action) {
            case WorkflowRequestAction.Amendment:
                return this.l(
                    'LeaveHistory_Amendment',
                    this.fmt(item.beforeStartDate),
                    this.fmt(item.beforeEndDate),
                    this.fmt(item.startDate),
                    this.fmt(item.endDate)
                );
            case WorkflowRequestAction.Cancellation:
                return this.l('LeaveHistory_Cancellation', this.fmt(item.startDate), this.fmt(item.endDate));
            case WorkflowRequestAction.Return:
                return this.l('LeaveHistory_Return', this.fmt(item.returnDate));
            default:
                return this.l('LeaveHistory_New', this.fmt(item.startDate), this.fmt(item.endDate), item.days ?? '');
        }
    }

    /** For a late return: the extra paid and unpaid ranges it books. Empty otherwise. */
    historyPaidUnpaid(item: MyLeaveHistoryItemDto): string {
        if (!item.paidStartDate && !item.unpaidStartDate) {
            return '';
        }

        const range = (from?: DateTime, to?: DateTime) => (from && to ? `${this.fmt(from)} – ${this.fmt(to)}` : '–');

        return this.l(
            'LeaveHistory_PaidUnpaid',
            range(item.paidStartDate, item.paidEndDate),
            range(item.unpaidStartDate, item.unpaidEndDate)
        );
    }

    historyOutcome(item: MyLeaveHistoryItemDto): string {
        return this.l('LeaveHistoryOutcome_' + MyLeaveHistoryOutcome[item.outcome]);
    }

    historyOutcomeClass(item: MyLeaveHistoryItemDto): string {
        switch (item.outcome) {
            case MyLeaveHistoryOutcome.Approved:
                return 'text-success';
            case MyLeaveHistoryOutcome.Rejected:
                return 'text-danger';
            case MyLeaveHistoryOutcome.AwaitingApproval:
                return 'text-warning';
            default:
                return 'text-muted';
        }
    }

    historyIcon(item: MyLeaveHistoryItemDto): string {
        switch (item.outcome) {
            case MyLeaveHistoryOutcome.Approved:
                return 'fa fa-check-circle text-success';
            case MyLeaveHistoryOutcome.Rejected:
                return 'fa fa-times-circle text-danger';
            case MyLeaveHistoryOutcome.AwaitingApproval:
                return 'fa fa-clock text-warning';
            case MyLeaveHistoryOutcome.SentBack:
                return 'fa fa-reply text-info';
            default:
                return 'fa fa-undo text-muted';
        }
    }

    historyDecidedBy(item: MyLeaveHistoryItemDto): string {
        const name = this.isRtl ? item.decidedByNameAr || item.decidedByNameEn : item.decidedByNameEn;
        return name ?? '';
    }

    // ------------------------------------------------------------------ small helpers

    fmt(value: DateTime | undefined): string {
        return value ? value.toFormat('dd/MM/yyyy') : '';
    }

    private toDate(value: string): DateTime | undefined {
        return value ? DateTime.fromISO(value) : undefined;
    }

    typeName(balance: MyLeaveBalanceDto): string {
        return this.isRtl ? balance.absenceTypeNameAr || balance.absenceTypeNameEn : balance.absenceTypeNameEn;
    }

    typeNameOf(type: MyLeaveTypeDto | undefined): string {
        return (this.isRtl ? type?.nameAr || type?.nameEn : type?.nameEn) ?? '';
    }

    leaveTypeName(leave: MyLeaveDto): string {
        return this.isRtl ? leave.absenceTypeNameAr || leave.absenceTypeNameEn : leave.absenceTypeNameEn;
    }

    /**
     * Fetches the signed-in employee, for the name on the detail dialog. Failure is swallowed: an account
     * with no employee record is already reported by the list call.
     */
    private loadContext(): void {
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

    /**
     * Opens a request read-only: its dates, the note the employee sent, the approvers' remarks, comments,
     * attachments and the trail -- the engine's own view of it.
     *
     * <p>Takes the workflow request explicitly rather than reading it off the row, because a leave with an
     * action in flight has two: its own, and the action's. The list and the history pass whichever one the
     * employee pressed.</p>
     */
    openDetail(leave: MyLeaveDto, workflowRequestId?: number): void {
        this.detail()?.show({
            title: this.l('EssMyLeave'),
            fields: [
                { label: this.l('Employee'), value: this.employeeDisplay() },
                { label: this.l('AbsenceType'), value: this.leaveTypeName(leave) },
                { label: this.l('StartDate'), value: this.fmt(leave.startDate) },
                { label: this.l('EndDate'), value: this.fmt(leave.endDate) },
                { label: this.l('Days'), value: `${leave.totalLeaveDays ?? ''}` },
            ],
            comments: leave.comments,
            requesterName: this.employeeDisplay(),
            workflowRequestId: workflowRequestId ?? leave.workflowRequestId,
            statusLabel: this.statusLabel(leave),
            statusClass: this.statusClass(leave),
        });
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
