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
