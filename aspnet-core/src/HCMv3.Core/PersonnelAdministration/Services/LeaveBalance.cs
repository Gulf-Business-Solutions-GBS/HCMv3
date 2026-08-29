namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// One absence type's balance for one employee, separating what has been CONSUMED from what is merely
/// SPOKEN FOR.
/// </summary>
public class LeaveBalance
{
    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeNameEn { get; set; }

    public string AbsenceTypeNameAr { get; set; }

    /// <summary>Accrued for the current entitlement period.</summary>
    public double Accrued { get; set; }

    /// <summary>Manual adjustments, positive or negative.</summary>
    public double Adjusted { get; set; }

    /// <summary>Lapsed at period end.</summary>
    public double Forfeited { get; set; }

    /// <summary>
    /// Days actually consumed by APPROVED leave. This is the number payroll and accrual use, and it is
    /// unaffected by anything still awaiting approval.
    /// </summary>
    public double Used { get; set; }

    /// <summary>
    /// Days on leave that is submitted but not yet decided. Not consumed -- it may still be rejected -- but
    /// it is spoken for, so it cannot be booked twice.
    /// </summary>
    public double Pending { get; set; }

    /// <summary>The quota balance in the traditional sense: what the employee has actually got left after
    /// approved leave. Shown so an employee can see why bookable is lower.</summary>
    public double Remaining => Accrued + Adjusted - Forfeited - Used;

    /// <summary>
    /// What the employee may still apply for. The number the ESS screen shows and the number an application
    /// is validated against.
    /// </summary>
    public double Bookable => Remaining - Pending;
}
