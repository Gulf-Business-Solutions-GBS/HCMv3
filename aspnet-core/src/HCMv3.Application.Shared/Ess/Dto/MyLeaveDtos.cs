using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.Ess.Dto;

/// <summary>One absence type's balance, as the employee sees it.</summary>
public class MyLeaveBalanceDto
{
    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeNameEn { get; set; }

    public string AbsenceTypeNameAr { get; set; }

    public double Accrued { get; set; }

    /// <summary>Consumed by approved leave.</summary>
    public double Used { get; set; }

    /// <summary>Submitted but not yet decided. Shown separately so the employee can see why bookable is
    /// lower than the balance they expected.</summary>
    public double Pending { get; set; }

    public double Remaining { get; set; }

    /// <summary>What they may still apply for. The number the apply form validates against.</summary>
    public double Bookable { get; set; }
}

public class MyLeaveDto : EntityDto<int>
{
    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeNameEn { get; set; }

    public string AbsenceTypeNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalLeaveDays { get; set; }

    public string Comments { get; set; }

    /// <summary>Null when the leave was entered directly by HR rather than requested.</summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }

    /// <summary>Whether the employee may still withdraw it -- false once it is approved or already
    /// decided.</summary>
    public bool CanCancel { get; set; }

    /// <summary>
    /// Whether a CHANGE can be requested to this leave. True only once it is approved: before that the
    /// employee should withdraw and re-apply, which costs nobody an approval, and an amendment to something
    /// nobody has agreed to yet is a change to a request rather than to a fact.
    /// </summary>
    public bool CanAmend { get; set; }

    /// <summary>
    /// True while a change to this leave is itself awaiting approval. The dates shown are still the approved
    /// ones -- an amendment does not touch the row until it is granted -- so without this the screen would
    /// look identical before and after asking for the change.
    /// </summary>
    public bool HasPendingAmendment { get; set; }

    /// <summary>The in-flight amendment request, so the screen can open its activity thread.</summary>
    public int? PendingAmendmentRequestId { get; set; }

    /// <summary>Whether the employee can ask for this APPROVED leave to be cancelled (legacy
    /// <c>ActionType.Cancellation</c>). Same conditions as <see cref="CanAmend"/>.</summary>
    public bool CanRequestCancellation { get; set; }

    /// <summary>Whether the employee can report their return from this leave: approved, not yet returned,
    /// and no other change in flight.</summary>
    public bool CanReturn { get; set; }

    /// <summary>True once the return from this leave has been approved.</summary>
    public bool IsReturned { get; set; }

    /// <summary>The day the employee came back, once a return is approved.</summary>
    public DateTime? ReturnDate { get; set; }

    /// <summary>The end date first approved, when an early return or a change has since moved it.</summary>
    public DateTime? InitialEndDate { get; set; }

    /// <summary>Hourly leave: the hours taken. Zero for a full-day leave.</summary>
    public double AbsenceHours { get; set; }

    /// <summary>Hourly leave: "HH:mm". Null for a full-day leave.</summary>
    public string StartTime { get; set; }

    /// <inheritdoc cref="StartTime"/>
    public string EndTime { get; set; }

    public HalfdayType HalfdayType { get; set; }

    /// <summary>
    /// True when this NEW leave was sent back by an approver and is waiting on the employee: they correct it
    /// and send it again, or withdraw it.
    /// </summary>
    public bool IsSentBack { get; set; }

    /// <summary>Which action is in flight against this approved leave: amendment, cancellation or return.</summary>
    public WorkflowRequestAction? PendingAction { get; set; }

    /// <summary>True when the in-flight action was sent back and is waiting on the employee.</summary>
    public bool PendingIsSentBack { get; set; }

    /// <summary>
    /// What the in-flight action would make of the leave. The leave's own dates above stay the approved ones
    /// until it is granted, so the list shows these beside them rather than in their place.
    /// </summary>
    public DateTime? ProposedStartDate { get; set; }

    /// <inheritdoc cref="ProposedStartDate"/>
    public DateTime? ProposedEndDate { get; set; }

    /// <summary>The rejoining date on an in-flight return.</summary>
    public DateTime? ProposedReturnDate { get; set; }

    /// <summary>
    /// The last action against this leave, when it was REJECTED and nothing has been raised since. The leave is
    /// exactly as it was; this is what lets the list say "amendment rejected" instead of looking untouched.
    /// </summary>
    public WorkflowRequestAction? LastRejectedAction { get; set; }

    /// <summary>
    /// The request the row is showing: the leave itself, or the latest amendment, cancellation or return raised
    /// against it. A request the employee withdrew is passed over -- it changed nothing -- and stays in the
    /// leave's history.
    /// </summary>
    public WorkflowRequestAction RequestAction { get; set; }

    /// <summary>
    /// The status of <see cref="RequestAction"/>, from the one set every self-service request uses. A leave
    /// whose cancellation was granted therefore reads "Cancel Leave / Approved".
    /// </summary>
    public MyLeaveRequestStatus DisplayStatus { get; set; }

    /// <summary>
    /// The leave's own number -- the one it was applied for under -- which it keeps for life. Later requests
    /// against it have numbers of their own, in <see cref="Requests"/>. Empty for a draft not yet sent.
    /// </summary>
    public string RequestNumber { get; set; }

    /// <summary>
    /// Every request raised against this leave, oldest first: the application itself, then each amendment,
    /// cancellation or return, with its own number. The list shows them under the leave's row.
    /// </summary>
    public List<MyLeaveHistoryItemDto> Requests { get; set; } = new();

    /// <summary>
    /// True for a leave the employee SAVED and has not sent yet: a draft only they can see. Different from
    /// <see cref="IsSentBack"/>, which is a leave an approver returned to them.
    /// </summary>
    public bool IsDraft { get; set; }

    /// <summary>True when the in-flight action is the employee's own unsent draft rather than one sent back.</summary>
    public bool PendingIsDraft { get; set; }

    /// <summary>What the employee wrote on the in-flight action, so reopening it shows the same words.</summary>
    public string PendingReason { get; set; }

    public int? ProposedAbsenceTypeId { get; set; }

    public DateTime? ProposedPaidStartDate { get; set; }

    public DateTime? ProposedPaidEndDate { get; set; }

    public DateTime? ProposedUnpaidStartDate { get; set; }

    public DateTime? ProposedUnpaidEndDate { get; set; }

    public double CalendarDays { get; set; }

    public LeavePaymentType LeavePaymentType { get; set; }

    public DateTime? PaymentDate { get; set; }

    public bool IsTravelling { get; set; }

    public int? PrimaryDelegateId { get; set; }

    public string PrimaryDelegateName { get; set; }

    public int? SecondaryDelegateId { get; set; }

    public string SecondaryDelegateName { get; set; }

    public int? ApproverDelegateId { get; set; }

    public string ApproverDelegateName { get; set; }

    public bool PolicyAccepted { get; set; }
}

/// <summary>A leave saved without sending it. <c>Id</c> is the draft being saved again, or empty for a new one.</summary>
public class SaveMyLeaveDraftInput : ApplyForLeaveInput
{
    public int? Id { get; set; }
}

/// <summary>
/// An amendment, cancellation or return saved without sending it. The approved leave is untouched; the draft
/// is the one request in flight against it until it is submitted or withdrawn.
/// </summary>
public class SaveMyLeaveChangeDraftInput : ResubmitMyLeaveChangeInput
{
    /// <summary>Which action is being drafted. Ignored when a draft already exists for the leave.</summary>
    public WorkflowRequestAction Action { get; set; } = WorkflowRequestAction.Amendment;
}

public class GetMyLeavesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>When false (the default) only in-flight and future leave is listed, which is what an
    /// employee usually wants; true includes their full history.</summary>
    public bool IncludeHistory { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}

public class ApplyForLeaveInput
{
    [Range(1, int.MaxValue)]
    public int AbsenceTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }

    /// <summary>
    /// How the leave is to be paid. <c>PayInAdvance</c> asks for the leave salary before the employee travels,
    /// which an off-cycle run then pays; the default, <c>PayWithPayroll</c>, pays it with the month as usual.
    /// </summary>
    public LeavePaymentType LeavePaymentType { get; set; } = LeavePaymentType.PayWithPayroll;

    /// <summary>
    /// When the advance should be paid. Left empty, the day legacy would choose is filled in: the 1st of the
    /// month for a leave starting on or before the 15th, the 16th for one starting later, when the payroll
    /// settings allow it. Recomputed on approval to the first working day from that point
    /// (<c>AbsenceRequest</c> lines 1305-1328), so it never lands on a week-off or a holiday.
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Take what the balance covers as paid leave and the rest as UNPAID leave, rather than being refused for
    /// insufficient balance (legacy's "additional unpaid leave", <c>AbsenceRequest.ExtAbsenceType</c>). The
    /// split itself is worked out by the server; <c>PreviewMyLeaveSplit</c> shows it before committing.
    /// </summary>
    public bool SplitExcessAsUnpaidLeave { get; set; }

    /// <summary>
    /// Files the employee uploaded with the request -- the tokens the upload endpoint returned. They are
    /// attached to the leave before it goes for approval, which is what lets an absence type that REQUIRES a
    /// document (a sick note, say) be submitted at all.
    /// </summary>
    public List<string> AttachmentFileTokens { get; set; } = new();

    /// <summary>Half-day leave, when the time settings offer it. The times are then taken from the day's duty.</summary>
    public HalfdayType HalfdayType { get; set; }

    /// <summary>
    /// Hourly leave: when it starts, as "HH:mm". Only for a one-day leave of a type that allows partial leave;
    /// leave both times empty for a full-day leave.
    /// </summary>
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string StartTime { get; set; }

    /// <inheritdoc cref="StartTime"/>
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string EndTime { get; set; }

    public bool IsTravelling { get; set; }

    /// <summary>Legacy "Primary Delegation": the colleague covering the employee's work.</summary>
    public int? PrimaryDelegateId { get; set; }

    /// <summary>Legacy "Secondary Delegation". Needs a primary delegate.</summary>
    public int? SecondaryDelegateId { get; set; }

    /// <summary>
    /// Legacy "Approver Delegation": who decides this employee's approvals while they are away. Becomes a real
    /// delegation for the leave dates once the leave is approved.
    /// </summary>
    public int? ApproverDelegateId { get; set; }

    /// <summary>The employee ticked "I accept the leave policy". Required by types that ask for it.</summary>
    public bool PolicyAccepted { get; set; }
}

/// <summary>
/// The tenant settings that shape the leave form, for the ESS and HR screens alike. Legacy read these one by
/// one from <c>ManageTimeSettings</c>, <c>ManagePayrollSettings</c> and <c>ManageWorkflowSettings</c> while
/// building the leave request page; the screen shows or hides fields and options from them, and the server
/// refuses what they switch off.
/// </summary>
public class LeaveFormSettingsDto
{
    /// <summary>Legacy <c>ManageTimeSettings.EnableAutoHalfDayLeave</c>: the half-day choice is offered.</summary>
    public bool EnableAutoHalfDayLeave { get; set; }

    /// <summary>Legacy <c>ManageTimeSettings.EnablePayFullWithPayroll</c>: "Pay Full With Payroll" is offered.</summary>
    public bool EnablePayFullWithPayroll { get; set; }

    /// <summary>Legacy <c>ManagePayrollSettings.DefaultLeavePaymentType</c>: the payment type the form starts on.</summary>
    public LeavePaymentType DefaultLeavePaymentType { get; set; }

    /// <summary>Legacy <c>ManageTimeSettings.EnableIsTravelling</c> (key SHOW_HIDE_IsTravelling): the travelling tick box is shown.</summary>
    public bool EnableIsTravelling { get; set; }

    /// <summary>Legacy <c>ManageTimeSettings.EnableBookingQuotaInLeaveRequest</c>: pending requests reduce the bookable balance.</summary>
    public bool EnableBookingQuotaInLeaveRequest { get; set; }

    /// <summary>Legacy <c>ManageWorkflowSettings.HideLeavePayDetailsInWorkflow</c>: approvers do not see leave pay amounts.</summary>
    public bool HideLeavePayDetailsInWorkflow { get; set; }

    /// <summary>
    /// Whether the approver-delegate field is offered: the employee is somebody's manager or holds a workflow
    /// role today, so there are approvals to hand over (legacy <c>ValidateDelegationVisibility</c>).
    /// </summary>
    public bool CanDelegateApprovals { get; set; }
}

/// <summary>
/// A leave type the signed-in employee may apply for -- already filtered by their gender, religion and salary
/// profile -- with the settings the form needs to show the right fields before anything is submitted.
/// </summary>
public class MyLeaveTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>The start and end time fields are offered.</summary>
    public bool AllowPartialLeave { get; set; }

    /// <summary><c>Partial</c> makes the times mandatory.</summary>
    public LeaveDurationType LeaveDurationType { get; set; }

    public bool IsAttachmentRequired { get; set; }

    public bool RequirePolicyAcceptance { get; set; }

    /// <summary>The policy document to link to, when one has been uploaded on the leave type.</summary>
    public int? PolicyAttachmentId { get; set; }

    public string PolicyFileName { get; set; }

    /// <summary>The earliest start date a new leave of this type may have today; null for no limit.</summary>
    public DateTime? EarliestStartDate { get; set; }

    /// <summary>The latest start date a new leave of this type may have today; null for no limit.</summary>
    public DateTime? LatestStartDate { get; set; }

    /// <summary>True when the employee holds a quota for this type, so the balance figures mean something.</summary>
    public bool HasQuota { get; set; }
}

/// <summary>
/// The three balance figures the form shows for the leave being entered. Nothing is saved by asking.
/// </summary>
public class LeaveBalancePreviewDto
{
    /// <summary>False for a type with no quota: there is no balance to show.</summary>
    public bool HasQuota { get; set; }

    /// <summary>Accrued less everything approved -- the actual balance today.</summary>
    public double CurrentBalance { get; set; }

    /// <summary>The current balance less what is awaiting approval -- what may still be applied for.</summary>
    public double Bookable { get; set; }

    /// <summary>Days this request would charge. For a change, the difference from what is already approved.</summary>
    public double DaysRequested { get; set; }

    /// <summary>Bookable less the days requested.</summary>
    public double BalanceAfter { get; set; }
}

public class PreviewLeaveBalanceInput : ApplyForLeaveInput
{
    /// <summary>The approved leave being changed, so its own days are credited back. Null for a new leave.</summary>
    public int? EmployeeAbsenceId { get; set; }
}

/// <summary>A new leave that was sent back, corrected and sent again.</summary>
public class ResubmitMyLeaveInput : ApplyForLeaveInput
{
    [Range(1, int.MaxValue)]
    public int EmployeeAbsenceId { get; set; }
}

/// <summary>
/// A change, cancellation or return that was sent back, corrected and sent again. Which of the three it is
/// comes from the request itself; only the fields that action uses are read.
/// </summary>
public class ResubmitMyLeaveChangeInput
{
    /// <summary>The approved leave the sent-back request is against.</summary>
    [Range(1, int.MaxValue)]
    public int EmployeeAbsenceId { get; set; }

    /// <summary>Amendment: the corrected values.</summary>
    public int? AbsenceTypeId { get; set; }

    /// <inheritdoc cref="AbsenceTypeId"/>
    public DateTime? StartDate { get; set; }

    /// <inheritdoc cref="AbsenceTypeId"/>
    public DateTime? EndDate { get; set; }

    [StringLength(2000)]
    public string Reason { get; set; }

    /// <summary>Return: the corrected rejoining date and, for a late return, the paid and unpaid ranges.</summary>
    public DateTime? RejoiningDate { get; set; }

    public DateTime? PaidStartDate { get; set; }

    public DateTime? PaidEndDate { get; set; }

    public DateTime? UnpaidStartDate { get; set; }

    public DateTime? UnpaidEndDate { get; set; }
}

public class WithdrawMyLeaveChangeInput : EntityDto<int>
{
    [StringLength(2000)]
    public string Reason { get; set; }
}

public class RequestLeaveCancellationInput : EntityDto<int>
{
    /// <summary>Why the leave is being cancelled. Shown to the approver.</summary>
    [StringLength(2000)]
    public string Reason { get; set; }
}

/// <summary>How one request against a leave ended, or where it stands.</summary>
/// <summary>
/// The statuses a self-service request is shown with -- the same words for every kind of request.
/// </summary>
public enum MyLeaveRequestStatus
{
    Draft = 0,
    Open = 1,
    Approved = 2,
    Rejected = 3,
    SentBack = 4,
    Withdrawn = 5
}

public enum MyLeaveHistoryOutcome
{
    AwaitingApproval = 0,
    Approved = 1,
    Rejected = 2,
    SentBack = 3,
    Withdrawn = 4,

    /// <summary>Saved by the employee and not sent yet.</summary>
    Draft = 5
}

/// <summary>
/// One request raised against a leave -- the application itself, or a later change, cancellation or return --
/// with what it asked for and how it ended. A leave's history is these in the order they were raised.
/// </summary>
public class MyLeaveHistoryItemDto
{
    public int WorkflowRequestId { get; set; }

    public string RequestNumber { get; set; }

    public WorkflowRequestAction Action { get; set; }

    public MyLeaveHistoryOutcome Outcome { get; set; }

    public DateTime SubmittedOn { get; set; }

    public DateTime? DecidedOn { get; set; }

    public string DecidedByNameEn { get; set; }

    public string DecidedByNameAr { get; set; }

    /// <summary>The remark given with the decision, or the reason given with a withdrawal.</summary>
    public string DecisionRemark { get; set; }

    /// <summary>What the employee wrote when raising it.</summary>
    public string Reason { get; set; }

    /// <summary>The leave as it stood when this was raised. Null on the original application.</summary>
    public DateTime? BeforeStartDate { get; set; }

    public DateTime? BeforeEndDate { get; set; }

    public double? BeforeDays { get; set; }

    /// <summary>What this request asked the leave to become.</summary>
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public double? Days { get; set; }

    /// <summary>Return only: the day the employee came back.</summary>
    public DateTime? ReturnDate { get; set; }

    /// <summary>Late return only: the ranges booked as further paid and unpaid leave.</summary>
    public DateTime? PaidStartDate { get; set; }

    public DateTime? PaidEndDate { get; set; }

    public DateTime? UnpaidStartDate { get; set; }

    public DateTime? UnpaidEndDate { get; set; }

    /// <summary>True when an approver sent it back at least once before it was decided.</summary>
    public bool WasSentBack { get; set; }
}

/// <summary>One stretch of a sick leave paid at the same rate, for the breakdown shown before applying.</summary>
public class SickLeaveDeductionSegmentDto
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public double DeductionNumber { get; set; }

    public DeductionUnit DeductionUnit { get; set; }

    /// <summary>True for days past the last slab, which are fully deducted.</summary>
    public bool IsBeyondTheLastSlab { get; set; }
}

/// <summary>
/// How a leave longer than the balance would be split: paid up to the balance, the remainder unpaid. Nothing is
/// saved by asking.
/// </summary>
public class LeaveSplitPreviewDto
{
    /// <summary>True when the leave fits the balance and no split is needed.</summary>
    public bool FitsWithinBalance { get; set; }

    public double Bookable { get; set; }

    /// <summary>Where the PAID part of the leave would now end.</summary>
    public DateTime? PaidEndDate { get; set; }

    public double PaidLeaveDays { get; set; }

    public DateTime? UnpaidStartDate { get; set; }

    public DateTime? UnpaidEndDate { get; set; }

    public double UnpaidLeaveDays { get; set; }

    /// <summary>Set when no unpaid leave type (code "2") is configured, so the split cannot be offered.</summary>
    public bool UnpaidLeaveTypeMissing { get; set; }
}

/// <summary>
/// What happened to an application. The two outcomes are genuinely different and the UI must say which:
/// routed for approval, or effective immediately.
/// </summary>
public class ApplyForLeaveResultDto
{
    public int EmployeeAbsenceId { get; set; }

    /// <summary>Null when the type has workflow switched off -- the leave is simply recorded.</summary>
    public int? WorkflowRequestId { get; set; }

    /// <summary>
    /// False when the type is not workflow-enabled, so the screen can say "recorded" rather than implying
    /// an approval that will never come.
    /// </summary>
    public bool SubmittedForApproval { get; set; }
}

/// <summary>
/// A request to CHANGE a leave that has already been approved.
///
/// <para>
/// The values here are what the leave should BECOME. They are not written to the leave until an approver
/// agrees -- until then the approved leave stands, and payroll and quota keep seeing it unchanged. This is
/// what stops an employee shortening their own approved leave, or quietly extending it, by editing a record
/// somebody already signed off.
/// </para>
/// </summary>
public class AmendMyLeaveInput
{
    /// <summary>The approved leave being changed.</summary>
    [Range(1, int.MaxValue)]
    public int EmployeeAbsenceId { get; set; }

    [Range(1, int.MaxValue)]
    public int AbsenceTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Why the change is being asked for. Required -- an approver deciding on a change to something
    /// they already granted needs to know what prompted it.</summary>
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Reason { get; set; }
}
