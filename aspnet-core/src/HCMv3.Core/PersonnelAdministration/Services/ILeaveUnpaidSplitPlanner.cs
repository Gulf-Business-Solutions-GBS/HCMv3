using System;
using System.Threading.Tasks;
using Abp.Domain.Services;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// How a leave longer than what is available would be split: the paid part up to the whole days available, the
/// remainder as additional unpaid leave. Nothing is saved.
/// </summary>
public class LeaveUnpaidSplitPlan
{
    /// <summary>Countable days of the whole leave as asked for (main type's exclusions).</summary>
    public double RequestedDays { get; set; }

    /// <summary>Whole days the paid part may take: floor of the available days, never negative.</summary>
    public double PaidDaysAllowed { get; set; }

    /// <summary>True when the leave already fits: no split.</summary>
    public bool FitsWithinBalance { get; set; }

    /// <summary>Where the paid part now ends; null when not a single whole day is available.</summary>
    public DateTime? PaidEndDate { get; set; }

    public double PaidLeaveDays { get; set; }

    public double PaidCalendarDays { get; set; }

    public DateTime? UnpaidStartDate { get; set; }

    public DateTime? UnpaidEndDate { get; set; }

    /// <summary>Counted with the MAIN type's week-off and holiday rules, as legacy did.</summary>
    public double UnpaidLeaveDays { get; set; }

    public double UnpaidCalendarDays { get; set; }
}

/// <summary>
/// The additional-unpaid-leave split shared by the employee's own application (ESS
/// <c>PreviewMyLeaveSplit</c>) and HR's leave form (<c>GetUnpaidSplitProposal</c>, legacy
/// <c>EmployeeAbsenceController.SetAdditionalUnpaidLeave</c>). Each caller decides how many days are available;
/// this works out where the paid part ends and what is left over.
/// </summary>
public interface ILeaveUnpaidSplitPlanner : IDomainService
{
    /// <param name="availableDays">Days the paid part may use; only whole days are taken (legacy floor).</param>
    /// <param name="excludeEmployeeAbsenceId">The leave being edited, so it does not block its own end date.</param>
    Task<LeaveUnpaidSplitPlan> PlanAsync(
        int employeeId, int absenceTypeId, DateTime startDate, DateTime endDate, double availableDays,
        int excludeEmployeeAbsenceId = 0);

    /// <summary>
    /// Legacy <c>SetAdditionalUnpaidLeave</c> (controller 801-804): a sick leave's excess goes to the type named
    /// "Sick Leave Unpaid", any other leave's to "Unpaid Leave"; the unpaid type by code ("2") when the named one
    /// is not set up. Null when none exists.
    /// </summary>
    AbsenceType FindAdditionalUnpaidLeaveType(AbsenceType mainType);
}
