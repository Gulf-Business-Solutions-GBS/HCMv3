using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Answers the two questions a missing-punch request has to answer before it can become a real punch: which
/// shift was this employee supposed to be working that day, and is the punch they are asking for an arrival
/// or a departure.
///
/// <para>
/// Both answers already exist elsewhere in the system, which is the point of this interface: it names the
/// combination rather than reimplementing either. The duty comes from
/// <see cref="HCMv3.PersonnelAdministration.Services.IEmployeeAbsenceCalculationService.GetEmployeeDutyAsync"/>,
/// which is the port of the legacy day-of-week resolution and already contains the roster branch
/// (<see cref="IRosterService.GetRosterDutyAsync"/>) and the substitute-duty override. Legacy
/// <c>SkipPunchRequest</c> wrote its own copy of that seven-case switch inside <c>OnSaving</c>; a second copy
/// would drift from the one leave calculation uses, and then a missing punch and a half-day leave on the
/// same date would disagree about what shift the employee was on.
/// </para>
/// </summary>
public interface IPunchDutyResolver : IDomainService
{
    /// <summary>
    /// The employee's duty for <paramref name="date"/>, or <c>null</c> when none can be resolved -- no duty
    /// period covering the date, no duty configured for that day of the week, or no roster entry.
    ///
    /// <para>
    /// Deliberately non-throwing where the underlying service throws, because "this employee was not
    /// rostered that day" is an ordinary answer to a request, not a fault: it is the reason to refuse the
    /// request with a message the employee can act on. Genuine configuration faults (a duty period with no
    /// week detail filled in, say) are still allowed to surface.
    /// </para>
    /// </summary>
    Task<Duty> TryResolveDutyAsync(int employeeId, DateTime date);

    /// <summary>
    /// Whether <paramref name="transTypeId"/> is the arrival punch, i.e. the one the Time Engine treats as
    /// clock-in (<see cref="TransTypeCodes.ClockIn"/>). Everything else is treated as a departure, matching
    /// the engine, which only ever asks "is this the clock-in type".
    /// </summary>
    Task<bool> IsClockInAsync(int transTypeId);

    /// <summary>
    /// The time a missing punch should be stamped with when the employee did not supply one: the duty's
    /// start for an arrival, its end for a departure. Returns <c>null</c> when no duty can be resolved, so
    /// the caller can refuse rather than invent a time.
    /// </summary>
    Task<TimeSpan?> ResolveDefaultPunchTimeAsync(int employeeId, int transTypeId, DateTime date);
}
