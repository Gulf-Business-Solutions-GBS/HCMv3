using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The values a leave is judged on, wherever it comes from: an ESS application, an amendment awaiting approval,
/// or a row HR is keying in. Legacy applied the same rule set in two places at once -- on the request
/// (<c>WorkFlowServices.SaveWorkflowRequest</c>) and on the leave row itself
/// (<c>EmployeeAbsence.PerformValidations</c> / <c>UpdateQuotaInformation</c>) -- so a leave could not reach the
/// table through any door without passing them.
/// </summary>
public class LeaveRuleContext
{
    public int EmployeeId { get; set; }

    public int AbsenceTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Days charged to the quota: calendar days less the days the type excludes. Zero means "work it out".</summary>
    public double TotalLeaveDays { get; set; }

    /// <summary>Hourly leave only; 0 for a full-day leave.</summary>
    public double AbsenceHours { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public HalfdayType HalfdayType { get; set; }

    /// <summary>The leave being edited, so it does not count as its own conflict. 0 for a new one.</summary>
    public int ExcludeEmployeeAbsenceId { get; set; }

    /// <summary>
    /// The leave row the rules are being applied to, when it exists. Only the attachment rule needs it: a
    /// required document is looked up against this record, and a leave that has not been saved yet cannot have
    /// one, so the rule is skipped for a row with no id.
    /// </summary>
    public int EmployeeAbsenceId { get; set; }

    /// <summary>True for a leave that does not exist yet. Legacy applies back-dating only to new rows.</summary>
    public bool IsNew { get; set; } = true;

    /// <summary>
    /// The day the request was raised, for a leave being edited: legacy measures back-dating from the row's
    /// creation date rather than from today, so an old request does not become invalid while it waits.
    /// </summary>
    public DateTime? CreatedOn { get; set; }

    /// <summary>
    /// Days already approved for this leave that the balance check must credit back (an amendment to a leave
    /// that is already in the quota's Used).
    /// </summary>
    public double DaysAlreadyCounted { get; set; }

    /// <summary>
    /// Extra days the same submission books against the same quota -- the paid part of a late return. Legacy
    /// compares the balance against <c>TotalLeaveDays + DeductFromQuota</c>.
    /// </summary>
    public double DeductFromQuota { get; set; }

    /// <summary>How the leave is paid. <c>PayInAdvance</c> requires a payment date.</summary>
    public LeavePaymentType LeavePaymentType { get; set; }

    /// <summary>When an advance is to be paid. Must fall within the month the leave starts in.</summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// What the employee wrote with the request. The company leave policy makes this mandatory when the leave
    /// breaks the advance-notice or maximum-days rule (legacy " Please Add your Comments.").
    /// </summary>
    public string Comments { get; set; }

    /// <summary>Legacy <c>DelegatedTo</c>: who covers the employee's work. Checked when set.</summary>
    public int? PrimaryDelegateId { get; set; }

    /// <summary>Legacy <c>SecondaryDelegation</c>. Cannot be given without a primary delegate.</summary>
    public int? SecondaryDelegateId { get; set; }

    /// <summary>Legacy <c>ApproverDelegation</c>: who decides the employee's approvals while they are away.</summary>
    public int? ApproverDelegateId { get; set; }

    /// <summary>
    /// Whether the employee ticked the leave-policy acceptance. Null means the question was not asked -- HR
    /// keying a leave in, or a change to a leave whose policy was accepted when it was first raised -- and the
    /// rule is skipped; false on a type that requires acceptance refuses the leave.
    /// </summary>
    public bool? PolicyAccepted { get; set; }

    /// <summary>The change being validated, so its own pending days are not counted against it.</summary>
    public int? ExcludeWorkflowRequestId { get; set; }
}

/// <summary>
/// Every leave rule legacy applied on submit, approval and save, in legacy's own order. Quota deduction itself
/// lives in the leave quota ledger; this only decides whether the leave is allowed.
/// </summary>
public interface ILeaveRuleValidator : IDomainService
{
    Task ValidateAsync(LeaveRuleContext context);
}
