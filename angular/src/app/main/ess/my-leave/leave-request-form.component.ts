import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { FileDownloadService } from '@shared/utils/file-download.service';
import {
    ApplyForLeaveInput,
    AttachmentServiceProxy,
    DeductionUnit,
    EssColleagueLookupDto,
    EssLookupServiceProxy,
    HalfdayType,
    LeaveBalancePreviewDto,
    LeaveDurationType,
    LeaveFormSettingsDto,
    LeavePaymentType,
    LeaveReturnKind,
    MyLeaveServiceProxy,
    MyLeaveTypeDto,
    PreviewLeaveBalanceInput,
    ReturnFromLeaveInput,
    ReturnFromLeavePreviewDto,
    SickLeaveDeductionSegmentDto,
    WorkflowRequestAction,
} from '@shared/service-proxies/service-proxies';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { DateTime } from 'luxon';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';
import { EssRequestFormMode } from '../my-exit-permit/exit-permit-form.component';

/** A colleague as the delegation search boxes show one: the id that is sent, and "1042 - Name". */
export interface LeaveDelegateOption {
    id: number;
    label: string;
}

/**
 * The leave request as the form holds it, for all four actions. Dates are ISO <c>yyyy-MM-dd</c> and times
 * <c>HH:mm</c>, because that is what the inputs read and write.
 */
export interface LeaveFormValue {
    /** New leave, or an amendment, cancellation or return against an approved one. */
    action: WorkflowRequestAction;

    /** "1042 - Ahmad Al Sayed". Shown, never chosen. */
    employeeDisplay: string;

    /** The leave being acted on: the approved one for amend / cancel / return, or the draft being reopened. */
    leaveId?: number;

    absenceTypeId: number | null;
    startDate: string;
    endDate: string;
    halfdayType: HalfdayType;
    startTime: string;
    endTime: string;
    leavePaymentType: LeavePaymentType;
    paymentDate: string;
    isTravelling: boolean;
    primaryDelegate: LeaveDelegateOption | null;
    secondaryDelegate: LeaveDelegateOption | null;
    approverDelegate: LeaveDelegateOption | null;
    policyAccepted: boolean;

    /** Request Comment: the note on a new leave, the reason on an amendment, cancellation or return. */
    comments: string;

    returnDate: string;
    paidStartDate: string;
    paidEndDate: string;
    unpaidStartDate: string;
    unpaidEndDate: string;

    /** The approved leave as it stands, shown above an amendment, cancellation or return. */
    currentLeave?: string;

    /** The days the approved leave already has charged, so an amendment can show its new total. */
    currentLeaveDays?: number;

    /** Read-only only: the words to show where a picker would be, and the figures the server worked out. */
    leaveTypeLabel?: string;
    halfdayLabel?: string;
    leavePaymentLabel?: string;
    calendarDays?: string;
    leaveDays?: string;
    currentBalance?: string;
    bookableBalance?: string;
    updatedBalance?: string;
}

/** An empty form for the given action. */
export function emptyLeaveForm(action = WorkflowRequestAction.New, employeeDisplay = ''): LeaveFormValue {
    return {
        action,
        employeeDisplay,
        absenceTypeId: null,
        startDate: '',
        endDate: '',
        halfdayType: HalfdayType.None,
        startTime: '',
        endTime: '',
        leavePaymentType: LeavePaymentType.PayWithPayroll,
        paymentDate: '',
        isTravelling: false,
        primaryDelegate: null,
        secondaryDelegate: null,
        approverDelegate: null,
        policyAccepted: false,
        comments: '',
        returnDate: '',
        paidStartDate: '',
        paidEndDate: '',
        unpaidStartDate: '',
        unpaidEndDate: '',
    };
}

/**
 * The leave request form -- the SAME form whether an employee is filling it in, looking at what they sent, or
 * an approver is deciding on it, exactly as the exit permit form is. Read-only means disabled, not
 * rearranged: every field keeps its place.
 *
 * <p>One form serves the four actions (legacy's unified leave request): a new leave shows every field; an
 * amendment lets the type and dates change; a cancellation shows the leave and asks only why; a return adds
 * the return date and, when late, the paid and unpaid date ranges.</p>
 *
 * <p>The balance sits on the right as "Leave Info", as legacy had it, and is re-asked of the server whenever
 * the leave type or a date changes. When the form is read-only the figures come with the request instead:
 * they are the REQUESTER's, which an approver's own session could not ask for.</p>
 */
@Component({
    selector: 'leave-request-form',
    templateUrl: './leave-request-form.component.html',
    imports: [DateInputDirective, CommonModule, FormsModule, LocalizePipe, AttachmentsComponent, AutoCompleteModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LeaveRequestFormComponent extends AppComponentBase implements OnInit {
    private readonly _myLeaveService = inject(MyLeaveServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);
    private readonly _attachmentService = inject(AttachmentServiceProxy);
    private readonly _download = inject(FileDownloadService);

    readonly mode = input<EssRequestFormMode>('create');

    /** The form's data. In create mode the host passes its own object and this writes into it through ngModel. */
    readonly value = input.required<LeaveFormValue>();

    /** Disabled while the host is saving, so a double Submit cannot raise two requests. */
    readonly saving = input(false);

    /** Save as draft: files the request without sending it to an approver. */
    readonly saved = output<void>();
    readonly submitted = output<void>();
    readonly cancelled = output<void>();

    readonly readOnly = computed(() => this.mode() !== 'create');

    readonly leaveTypes = signal<MyLeaveTypeDto[]>([]);
    readonly colleagues = signal<EssColleagueLookupDto[]>([]);
    readonly settings = signal<LeaveFormSettingsDto | undefined>(undefined);
    readonly delegateSuggestions = signal<LeaveDelegateOption[]>([]);

    readonly balance = signal<LeaveBalancePreviewDto | undefined>(undefined);
    readonly sickLeaveSegments = signal<SickLeaveDeductionSegmentDto[]>([]);
    readonly returnPreview = signal<ReturnFromLeavePreviewDto | undefined>(undefined);

    /** Bumped when a plain form field changes, so the computeds below notice. */
    private readonly version = signal(0);

    private readonly attachments = viewChild<AttachmentsComponent>('formAttachments');

    readonly Action = WorkflowRequestAction;
    readonly Halfday = HalfdayType;
    readonly PaymentType = LeavePaymentType;
    readonly ReturnKind = LeaveReturnKind;
    readonly DeductionUnitPercentage = DeductionUnit.Percentage;

    readonly selectedType = computed(() => {
        this.version();
        return this.leaveTypes().find((t) => t.id === this.value().absenceTypeId);
    });

    constructor() {
        super();

        // A different request handed to the same form (the host opening another row): ask again.
        effect(() => {
            const value = this.value();
            if (!this.readOnly() && value) {
                queueMicrotask(() => this.refresh());
            }
        });
    }

    ngOnInit(): void {
        if (this.readOnly()) {
            return;
        }

        // Already filtered by the server to what THIS employee may apply for: gender, religion, salary profile.
        this._myLeaveService.getMyLeaveTypes().subscribe({
            next: (result) => {
                this.leaveTypes.set(result ?? []);
                this.version.update((v) => v + 1);
            },
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

    // ------------------------------------------------------------------ what is shown

    isNew(): boolean {
        return this.value().action === WorkflowRequestAction.New;
    }

    isAmendment(): boolean {
        return this.value().action === WorkflowRequestAction.Amendment;
    }

    isCancellation(): boolean {
        return this.value().action === WorkflowRequestAction.Cancellation;
    }

    isReturn(): boolean {
        return this.value().action === WorkflowRequestAction.Return;
    }

    /** The leave type and dates can be typed into for a new leave and an amendment only. */
    datesEditable(): boolean {
        return !this.readOnly() && (this.isNew() || this.isAmendment());
    }

    showTimes(): boolean {
        if (this.readOnly()) {
            return !!this.value().startTime;
        }

        return this.isNew() && !!this.selectedType()?.allowPartialLeave;
    }

    timesEnabled(): boolean {
        const v = this.value();
        return !!v.startDate && v.startDate === v.endDate && v.halfdayType === HalfdayType.None;
    }

    timesRequired(): boolean {
        return this.selectedType()?.leaveDurationType === LeaveDurationType.Partial;
    }

    showHalfDay(): boolean {
        if (this.readOnly()) {
            return !!this.value().halfdayLabel;
        }

        return this.showTimes() && !!this.settings()?.enableAutoHalfDayLeave;
    }

    showTravelling(): boolean {
        return this.readOnly() ? this.value().isTravelling : !!this.settings()?.enableIsTravelling;
    }

    showPaymentDate(): boolean {
        return this.readOnly() ? !!this.value().paymentDate : this.value().leavePaymentType === LeavePaymentType.PayInAdvance;
    }

    showApproverDelegation(): boolean {
        return this.readOnly() ? !!this.value().approverDelegate : !!this.settings()?.canDelegateApprovals;
    }

    isLateReturn(): boolean {
        if (this.readOnly()) {
            return !!this.value().paidStartDate || !!this.value().unpaidStartDate;
        }

        return this.returnPreview()?.kind === LeaveReturnKind.Late;
    }

    typeNameOf(type: MyLeaveTypeDto | undefined): string {
        return (this.isRtl ? type?.nameAr || type?.nameEn : type?.nameEn) ?? '';
    }

    /** A stored date shown the way the rest of the system shows one, as text so no time zone can move it. */
    asDayMonthYear(iso: string | undefined): string {
        const [year, month, day] = (iso ?? '').split('-');
        return year && month && day ? `${day.substring(0, 2)}/${month}/${year}` : '';
    }

    requestCommentLabel(): string {
        return this.l(this.isCancellation() ? 'ReasonForCancellation' : this.isNew() ? 'RequestComment' : 'ReasonForTheChange');
    }

    // ------------------------------------------------------------------ Leave Info (the right-hand panel)

    info(): { calendarDays: string; leaveDays: string; current: string; bookable: string; updated: string } | undefined {
        const v = this.value();

        if (this.readOnly()) {
            if (!v.currentBalance && !v.leaveDays) {
                return undefined;
            }

            return {
                calendarDays: v.calendarDays ?? '',
                leaveDays: v.leaveDays ?? '',
                current: v.currentBalance ?? '',
                bookable: v.bookableBalance ?? '',
                updated: v.updatedBalance ?? '',
            };
        }

        const preview = this.balance();
        const calendar =
            v.startDate && v.endDate && v.endDate >= v.startDate
                ? `${Math.round(DateTime.fromISO(v.endDate).diff(DateTime.fromISO(v.startDate), 'days').days) + 1}`
                : '';

        const round = (n: number | undefined) => (n === undefined || n === null ? '' : `${Math.round(n * 100) / 100}`);

        return {
            calendarDays: calendar,
            // The server answers with what the request would CHARGE; for an amendment that is the difference,
            // so the days already approved are added back to show the leave's new length.
            leaveDays: preview ? round((preview.daysRequested ?? 0) + (this.isAmendment() ? v.currentLeaveDays ?? 0 : 0)) : '',
            current: preview?.hasQuota ? round(preview.currentBalance) : '',
            bookable: preview?.hasQuota ? round(preview.bookable) : '',
            updated: preview?.hasQuota ? round(preview.balanceAfter) : '',
        };
    }

    // ------------------------------------------------------------------ reacting to the employee's entries

    onTypeOrDatesChanged(): void {
        const v = this.value();

        // Times belong to a one-day leave; a range clears them, as legacy did.
        if (v.startDate !== v.endDate) {
            v.startTime = '';
            v.endTime = '';
            v.halfdayType = HalfdayType.None;
        }

        this.refresh();
    }

    onHalfdayChanged(): void {
        const v = this.value();
        if (v.halfdayType !== HalfdayType.None) {
            v.startTime = '';
            v.endTime = '';
        }

        this.refresh();
    }

    /**
     * Re-asks the server about this leave: the balance figures and, for sick leave, how it would be paid.
     * The server decides, not the screen -- what counts as a day off and how much is already booked are rules
     * it owns, and a second answer worked out here would lose to it at submit.
     */
    refresh(): void {
        this.version.update((n) => n + 1);

        if (this.readOnly()) {
            return;
        }

        const v = this.value();

        if (this.isReturn()) {
            this.previewReturn();
        }

        if (!v.absenceTypeId) {
            this.balance.set(undefined);
            this.sickLeaveSegments.set([]);
            return;
        }

        const input = new PreviewLeaveBalanceInput();
        input.absenceTypeId = v.absenceTypeId;
        input.startDate = this.toDate(v.startDate) as DateTime;
        input.endDate = this.toDate(v.endDate) as DateTime;
        input.startTime = this.isNew() && this.timesEnabled() ? v.startTime || undefined : undefined;
        input.endTime = this.isNew() && this.timesEnabled() ? v.endTime || undefined : undefined;

        // A change to an approved leave is charged only for the difference from what is already approved.
        input.employeeAbsenceId = this.isAmendment() ? v.leaveId : undefined;

        this._myLeaveService.previewMyLeaveBalance(input).subscribe({
            next: (preview) => this.balance.set(preview),
            error: () => this.balance.set(undefined),
        });

        if (!this.isNew() || !v.startDate || !v.endDate) {
            this.sickLeaveSegments.set([]);
            return;
        }

        const sick = new ApplyForLeaveInput();
        sick.absenceTypeId = v.absenceTypeId;
        sick.startDate = DateTime.fromISO(v.startDate);
        sick.endDate = DateTime.fromISO(v.endDate);

        this._myLeaveService.previewMySickLeaveDeduction(sick).subscribe({
            next: (segments) => this.sickLeaveSegments.set(segments ?? []),

            // An explanation of the request, not part of it: failing to fetch it must not stop anybody applying.
            error: () => this.sickLeaveSegments.set([]),
        });
    }

    /**
     * Asks the server what the return date means -- on time, early or late -- and for a late return brings
     * the paid and unpaid ranges it proposes, which the employee may then adjust.
     */
    previewReturn(): void {
        const v = this.value();
        if (!v.leaveId || !v.returnDate) {
            this.returnPreview.set(undefined);
            return;
        }

        const input = new ReturnFromLeaveInput();
        input.employeeAbsenceId = v.leaveId;
        input.rejoiningDate = DateTime.fromISO(v.returnDate);

        this._myLeaveService.previewReturnFromMyLeave(input).subscribe({
            next: (preview) => {
                this.returnPreview.set(preview);
                v.paidStartDate = preview.paidStartDate?.toISODate() ?? '';
                v.paidEndDate = preview.paidEndDate?.toISODate() ?? '';
                v.unpaidStartDate = preview.unpaidStartDate?.toISODate() ?? '';
                v.unpaidEndDate = preview.unpaidEndDate?.toISODate() ?? '';
                this.version.update((n) => n + 1);
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

    // ------------------------------------------------------------------ delegation

    colleagueName(colleague: EssColleagueLookupDto): string {
        const name = this.isRtl ? colleague.fullNameAr || colleague.fullNameEn : colleague.fullNameEn;
        return [colleague.code, name].filter((p) => !!p).join(' - ');
    }

    /** Type-to-search by staff number or name, first twenty matches, as legacy's picker offered. */
    searchDelegates(event: { query: string }, forSecondary = false): void {
        const query = (event.query ?? '').trim().toLowerCase();
        const excludedId = forSecondary ? this.value().primaryDelegate?.id : undefined;

        const matches: LeaveDelegateOption[] = [];
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
        const v = this.value();

        // Legacy: a secondary delegation cannot stand without a primary one, nor be the same person.
        if (!v.primaryDelegate || v.secondaryDelegate?.id === v.primaryDelegate.id) {
            v.secondaryDelegate = null;
        }

        this.version.update((n) => n + 1);
    }

    // ------------------------------------------------------------------ policy and attachments

    openPolicy(): void {
        const id = this.selectedType()?.policyAttachmentId;
        if (!id) {
            return;
        }

        this._attachmentService.getAttachmentDownload(id).subscribe((file) => this._download.downloadTempFile(file));
    }

    /** How many files are waiting on the form, so the host can tell a required document is missing. */
    pendingAttachmentCount(): number {
        return this.attachments()?.pending().length ?? 0;
    }

    /** Uploads the chosen files and returns their tokens, to be sent WITH the request. */
    async uploadAttachments(): Promise<string[]> {
        return (await this.attachments()?.uploadPendingToTokens()) ?? [];
    }

    clearAttachments(): void {
        this.attachments()?.clearPending();
    }

    private toDate(value: string): DateTime | undefined {
        return value ? DateTime.fromISO(value) : undefined;
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
