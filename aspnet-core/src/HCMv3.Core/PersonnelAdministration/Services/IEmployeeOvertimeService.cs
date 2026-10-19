using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The rule every <see cref="EmployeeOvertime"/> writer shares: one overtime row per employee per day
/// (legacy <c>EmployeeOvertime.PerformValidations</c>, "Record already exists").
///
/// <para>
/// Two writers use it: the Overtime tab (<c>EmployeeOvertimesAppService</c>) and an approved overtime batch
/// (<see cref="IOvertimeBatchService"/>). In legacy both ended in <c>EmployeeOvertime.OnSaving</c>, so an
/// approved <c>OvertimeRequest</c> that hit a day already recorded failed its approval; keeping the rule in
/// one place keeps that true here.
/// </para>
///
/// <para>
/// Reads with the <c>ApprovedOnly</c> filter as the caller has it, which is how the Overtime tab has always
/// checked: a claim still waiting for approval does not block another row for the same day.
/// </para>
/// </summary>
public interface IEmployeeOvertimeService
{
    /// <summary>Throws <see cref="Abp.UI.UserFriendlyException"/> when the employee already has overtime on that day.</summary>
    /// <param name="excludeId">The row being edited, so it does not count as its own duplicate; 0 for a new row.</param>
    Task EnsureNoOvertimeOnDateAsync(int employeeId, DateTime effectiveDate, int excludeId = 0);

    /// <summary>
    /// The (employee, day) pairs in the range that already have an overtime row, so a screen can say so before
    /// anybody builds a batch around them.
    /// </summary>
    Task<HashSet<(int EmployeeId, DateTime Date)>> GetDaysWithOvertimeAsync(
        IReadOnlyCollection<int> employeeIds,
        DateTime startDate,
        DateTime endDate);
}
