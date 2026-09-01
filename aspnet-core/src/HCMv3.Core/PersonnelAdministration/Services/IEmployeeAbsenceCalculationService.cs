using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;
using HCMv3.PayrollManagement;
using HCMv3.TimeManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Leave-day calculation engine for <see cref="EmployeeAbsence"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAbsence</c>'s static calculation-engine
/// region -- a ~4000-line XPO business-object file that mixes entity properties with this calculation
/// engine; only the calculation engine is ported here, per this port's "Group F" scope). See
/// <see cref="EmployeeAbsenceCalculationService"/> remarks for the full list of adaptations.
/// </summary>
public interface IEmployeeAbsenceCalculationService : IDomainService
{
    /// <summary>
    /// Ported verbatim (pure function, no DB access) from legacy
    /// <c>EmployeeAbsence.GetRecoveryPeriods(DateTime payDate, DateTime offCycleEndDate)</c>: returns the
    /// distinct first-of-month dates covering <paramref name="payDate"/>'s month through
    /// <paramref name="offCycleEndDate"/>.
    /// </summary>
    List<DateTime> GetRecoveryPeriods(DateTime payDate, DateTime offCycleEndDate);

    /// <summary>
    /// Ported from legacy <c>EmployeeAbsence.ActualLeaveDays(Employee, DateTime StartDate, DateTime EndDate,
    /// AbsenceType AbsType, Session, List&lt;EmployeeSubstituteDuty&gt; empSubstituteDutyList = null)</c>.
    /// Returns the count of excluded (weekend/public-holiday/rest-day, per <paramref name="absenceTypeId"/>'s
    /// Include* flags) calendar days within [<paramref name="startDate"/>, <paramref name="endDate"/>], with
    /// the "full month leave" 26-fixed-days adjustment applied when
    /// <c>ManagePayrollSettings.FullMonthLeaveAdjForFixedDays</c> is enabled, the AbsenceType is Annual Leave
    /// (Code "1" or <see cref="QuotaType.AnnualLeave"/>), and the window spans more than 27 days.
    /// </summary>
    Task<decimal> ActualLeaveDaysAsync(int employeeId, DateTime startDate, DateTime endDate, int? absenceTypeId, List<EmployeeSubstituteDuty> empSubstituteDutyList = null);

    /// <summary>
    /// Ported from legacy <c>EmployeeAbsence.GetHoulyLeaveDay(Employee emp, AbsenceType absenceType, DateTime
    /// LeaveDaydate, double absenceHours, Session session, bool OnlyMessage = false)</c> (legacy method name
    /// typo "Houly" preserved for traceability back to the legacy source). Returns the fraction of a full
    /// duty-day that <paramref name="absenceHours"/> represents on <paramref name="leaveDayDate"/>, based on
    /// the employee's resolved <see cref="Duty"/> planned working minutes (shift minus break).
    ///
    /// NOT PORTED (deliberately, see <see cref="EmployeeAbsenceCalculationService"/> remarks): the legacy
    /// <c>AbsenceType</c>/<c>OnlyMessage</c> parameters, whose only effect in the legacy source was to
    /// optionally report a UI-only "duty not found / weekly-off / rest-day" message via the
    /// <c>ErrorHandling.Instance.SetPageError</c> singleton (when <c>OnlyMessage</c> was true) or to
    /// construct-but-never-throw a bare <c>new Exception(msg)</c> (when <c>OnlyMessage</c> was false) --
    /// dead code in the legacy source with no effect on control flow or the returned value either way.
    /// </summary>
    Task<double> GetHoulyLeaveDayAsync(int employeeId, DateTime leaveDayDate, double absenceHours);

    /// <summary>Ported from legacy <c>EmployeeAbsence.GetEmployeeDuty(Employee emp, DateTime LeaveDaydate, Session session)</c>.</summary>
    Task<Duty> GetEmployeeDutyAsync(int employeeId, DateTime leaveDayDate);

    /// <summary>Ported from legacy <c>EmployeeAbsence.GetEmployeeDutyPeriod(Employee emp, DateTime LeaveDaydate, Session session)</c>.</summary>
    Task<DutyPeriod> GetEmployeeDutyPeriodAsync(int employeeId, DateTime leaveDayDate);

    /// <summary>
    /// Ported from legacy <c>EmployeeAbsence.RecalculateTotalLeaveDays</c> (an XPO auto-recalculating
    /// property hook at legacy line 2714), as an explicit on-demand service method per this port's
    /// established convention (see <see cref="HCMv3.TravelManagement"/>'s
    /// <c>CalculateExpectedPerDiemAsync</c>/<see cref="HCMv3.ProjectManagement"/>'s
    /// <c>CalculateWorkDaysAsync</c>), rather than as an entity property side-effect.
    /// </summary>
    Task<double> CalculateTotalLeaveDaysAsync(int employeeId, int absenceTypeId, DateTime startDate, DateTime endDate, double absenceHours);

    /// <summary>Convenience overload of <see cref="CalculateTotalLeaveDaysAsync(int,int,DateTime,DateTime,double)"/> taking the fields directly off an <see cref="EmployeeAbsence"/>.</summary>
    Task<double> CalculateTotalLeaveDaysAsync(EmployeeAbsence employeeAbsence);

    /// <summary>
    /// Ported from legacy <c>EmployeeAbsence.CalcLeaveEndDate(DateTime endDate, DateTime rejoindate, double
    /// dQuota, Employee emp, AbsenceType abstype, Session session)</c>. Walks forward from the day after
    /// <paramref name="endDate"/> and returns the date on which the <paramref name="quotaDays"/>-th
    /// COUNTABLE day falls -- skipping public holidays, and skipping weekly-offs/rest-days unless the day's
    /// duty allows a transaction to end on one (<c>Duty.AllowTransEnd</c>), and skipping any day on which
    /// another of the employee's absences starts.
    ///
    /// <para>
    /// This is the date PROJECTOR the rest of this service was missing: everything else here counts days
    /// within a window the caller already knows, while this answers "given N days of leave starting here,
    /// which calendar date does it end on". Useful well beyond return-from-leave -- extending a leave,
    /// converting excess days to unpaid, and any screen that offers an end date from a day count all need it.
    /// </para>
    /// <para>
    /// Returns <c>null</c> when no such date can be reached (legacy returned <c>default(DateTime)</c>), which
    /// includes <paramref name="quotaDays"/> being zero or negative -- legacy's <c>while (cnt &lt; dQuota)</c>
    /// never entered its loop in that case either.
    /// </para>
    /// <para>
    /// The legacy <c>rejoindate</c> parameter is not ported: it is accepted and then never read anywhere in
    /// the legacy body. Legacy also had no bound on its loop, so a configuration in which no day is ever
    /// countable would hang the request forever; this port gives up after ten years of calendar and returns
    /// <c>null</c>.
    /// </para>
    /// </summary>
    /// <param name="excludeEmployeeAbsenceId">An absence to ignore when checking whether another leave already starts on a candidate day -- normally the one being extended or shortened.</param>
    Task<DateTime?> CalcLeaveEndDateAsync(int employeeId, int? absenceTypeId, DateTime endDate, double quotaDays, int excludeEmployeeAbsenceId = 0);

    /// <summary>
    /// Ported from legacy <c>EmployeeAbsence.GetRegularDate(DateTime RejoinDate, double Quota, Employee
    /// Employee, AbsenceType AbsType, Session Session, Int32 _Oid, ref string message)</c>. Returns the last
    /// day of leave implied by an employee rejoining on <paramref name="rejoinDate"/> -- the day before it --
    /// or <c>null</c> when another of the employee's absences already starts on that day, which would make
    /// the rejoining date impossible.
    ///
    /// <para>
    /// NOT PORTED: legacy's <c>Quota</c> and <c>AbsType</c> parameters, and its <c>ref string message</c>.
    /// The holiday/weekend/rest-day branches that were the only readers of <c>AbsType</c> are commented out
    /// in the legacy source, and <c>Quota</c> is never read at all; what survives is the overlap check. The
    /// single message legacy could produce said the end date overlapped, which is exactly what <c>null</c>
    /// now says -- the caller owns the wording, since a domain service has no business composing a
    /// user-facing string in one language.
    /// </para>
    /// </summary>
    /// <param name="excludeEmployeeAbsenceId">The absence being returned from, which must not count as its own overlap.</param>
    Task<DateTime?> GetRegularDateAsync(int employeeId, DateTime rejoinDate, int excludeEmployeeAbsenceId = 0);
}
