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
}
