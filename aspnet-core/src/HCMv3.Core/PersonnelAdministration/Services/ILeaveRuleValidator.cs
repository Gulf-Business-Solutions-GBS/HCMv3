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
}

/// <summary>
/// Every leave rule legacy applied on submit, approval and save, in legacy's own order. Quota deduction itself
/// lives in the leave quota ledger; this only decides whether the leave is allowed.
/// </summary>
public interface ILeaveRuleValidator : IDomainService
{
    Task ValidateAsync(LeaveRuleContext context);
}
