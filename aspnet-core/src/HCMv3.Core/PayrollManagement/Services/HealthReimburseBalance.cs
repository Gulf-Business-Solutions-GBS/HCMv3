using System;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// One employee's health-reimbursement position for one entitlement window, separating what has been
/// CONSUMED from what is merely SPOKEN FOR -- the same split <c>LeaveBalance</c> makes.
/// </summary>
public class HealthReimburseBalance
{
    /// <summary>
    /// False when the employee has no sub-group assignment covering the date, or their sub-group carries no
    /// health-reimbursement tiers for it. Legacy refused the claim outright in that case ("No Health
    /// Reimburse Amount assigned. Please check Organization Assignment"), and so does this port: an absent
    /// entitlement is a configuration gap, not an unlimited allowance.
    /// </summary>
    public bool HasEntitlement { get; set; }

    public int? EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupNameEn { get; set; }

    public string EmployeeSubGroupNameAr { get; set; }

    /// <summary>Start of the deduction window (legacy <c>AccrualService.GetHealthReimbAccrualStartEnd</c>).</summary>
    public DateTime PeriodFrom { get; set; }

    /// <summary>End of the deduction window.</summary>
    public DateTime PeriodTo { get; set; }

    /// <summary>The sub-group's health-reimbursement tiers summed over the payment date, before carry-forward
    /// and before the cap.</summary>
    public double Entitlement { get; set; }

    /// <summary>Unspent balance rolled in from prior years -- zero unless
    /// <c>ManagePayrollSettings.AllowHealthReimburseBalCarryFwd</c> is on.</summary>
    public double CarriedForward { get; set; }

    /// <summary><c>EmployeeSubGroup.MaxHealthReimburseAmount</c>. Zero means no cap.</summary>
    public double Cap { get; set; }

    /// <summary>Entitlement plus carry-forward, capped. What the employee is allowed to spend in this
    /// window.</summary>
    public double Quota { get; set; }

    /// <summary>
    /// Spent by claims the rest of the system can see -- approved ones, plus any recorded directly by HR
    /// while the transaction type is dormant. This is the figure payroll agrees with.
    /// </summary>
    public double Used { get; set; }

    /// <summary>
    /// On claims that are submitted but not yet decided. Not spent -- they may still be rejected -- but
    /// spoken for, so the same money cannot be claimed twice.
    /// </summary>
    public double Pending { get; set; }

    /// <summary>The balance in the traditional sense: what is left after approved claims. Shown so an
    /// employee can see why the claimable figure is lower.</summary>
    public double Remaining => Math.Round(Quota - Used, 3);

    /// <summary>What the employee may still claim -- the number the ESS screen shows and the number a new
    /// claim is validated against.</summary>
    public double Claimable => Math.Round(Quota - Used - Pending, 3);
}
