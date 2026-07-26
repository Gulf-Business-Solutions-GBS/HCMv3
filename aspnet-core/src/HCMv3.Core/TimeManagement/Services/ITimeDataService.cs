using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Holiday/weekend/rest-day resolution engine for time &amp; attendance (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.TimeDataService</c>, a static class -- ported here as an
/// <see cref="IDomainService"/> per this port's convention, since it needs injected <c>IRepository&lt;T&gt;</c>
/// instances instead of an XPO <c>Session</c>).
///
/// PORTING SCOPE: only the CURRENT (non-"_Old") variants of <c>GetPublicHolidaysdates</c>/
/// <c>GetPublicHolidaysdatesList</c> are ported (<c>GetPublicHolidaysdates_Old</c>/<c>_List_Old</c> are
/// superseded, slower day-by-day loop equivalents of the same logic that are never called by the current
/// variants -- skipped as dead weight, not meaningfully different in behavior).
///
/// ROSTER ENGINE ADAPTATION (documented, since the legacy <c>RosterService</c> class this file depends on
/// is not present in the legacy source available for this port): <c>RosterService.GetRosterDutyPeriod</c>/
/// <c>GetRosterDuty</c> are reconstructed here from the ported entity shapes actually available --
/// <see cref="RosterGroupDetail"/> resolves the Employee's active <see cref="RosterGroup"/> for a date, and
/// <see cref="RosterGroupSchedule"/> resolves that RosterGroup's active classic <see cref="DutyPeriod"/> for
/// the date (RosterGroupSchedule's own FK genuinely targets the classic DutyPeriod entity, not
/// RosterDutyPeriod -- verified when DutyPeriod/RosterGroupSchedule were ported). From there, both the
/// Roster and classic Weekly branches resolve the same way: the DutyPeriod's day-of-week Duty FK
/// (Sunday..Saturday), exactly mirroring the classic Weekly branch's own resolution logic in the legacy
/// source. This is the most faithful reconstruction possible without the actual legacy RosterService
/// source; DutyPeriodDetail's multi-week rotation (which some legacy Roster configurations may have used)
/// is NOT replicated here, since the RosterService algorithm that would have consumed it is unavailable.
/// </summary>
public interface ITimeDataService : IDomainService
{
    /// <summary>Ported from legacy <c>TimeDataService.IsHoliday</c>. Returns <c>true</c> when the Duty needed
    /// for <see cref="Employee"/> can be resolved and <paramref name="dayDate"/> is a public holiday for it, subject
    /// to <paramref name="absenceType"/>.IncludePublicHoliday (adapted from the legacy DateTime-sentinel return
    /// value to a plain <c>bool</c>).</summary>
    Task<bool> IsHolidayAsync(DateTime dayDate, AbsenceType absenceType, Employee employee);

    /// <summary>Ported from legacy <c>TimeDataService.GetPublicHolidaysdates</c> (current, non-"_Old" variant).</summary>
    Task<List<DateTime>> GetPublicHolidaysDatesAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeDutyPeriod> empDutyPeriodList = null);

    /// <summary>Ported from legacy <c>TimeDataService.GetPublicHolidaysdatesList</c> (current, non-"_Old" variant).
    /// Adapted from <c>List&lt;Tuple&lt;DateTime,string&gt;&gt;</c> to a value-tuple list.</summary>
    Task<List<(DateTime HolidayDate, string HolidayName)>> GetPublicHolidaysDatesListAsync(int employeeId, DateTime startDate, DateTime endDate);

    /// <summary>Ported from legacy <c>TimeDataService.IsWeekEnd</c> (bool-adapted, see <see cref="IsHolidayAsync"/>).</summary>
    Task<bool> IsWeekEndAsync(DateTime dayDate, AbsenceType absenceType, Employee employee);

    /// <summary>Ported from legacy <c>TimeDataService.GetWeeklyOffDaysCount</c>.</summary>
    Task<double> GetWeeklyOffDaysCountAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, List<EmployeeDutyPeriod> empDutyPeriodList = null);

    /// <summary>Ported from legacy <c>TimeDataService.GetWeeklyOffdateLIst</c>.</summary>
    Task<List<DateTime>> GetWeeklyOffDatesListAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, List<EmployeeDutyPeriod> empDutyPeriodList = null);

    /// <summary>Ported from legacy <c>TimeDataService.IsRestDays</c> (bool-adapted, see <see cref="IsHolidayAsync"/>).</summary>
    Task<bool> IsRestDayAsync(DateTime dayDate, AbsenceType absenceType, Employee employee);

    /// <summary>Ported from legacy <c>TimeDataService.GetRestDaysCount</c>.</summary>
    Task<double> GetRestDaysCountAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, List<EmployeeDutyPeriod> empDutyPeriodList = null);

    /// <summary>Ported from legacy <c>TimeDataService.GetRestDaysList</c>.</summary>
    Task<List<DateTime>> GetRestDaysListAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, List<EmployeeDutyPeriod> empDutyPeriodList = null);

    /// <summary>Ported from legacy <c>TimeDataService.RegularDay</c>. Adapted from the legacy DateTime-sentinel
    /// return value (<c>default(DateTime)</c> meaning "not found") to a nullable <see cref="DateTime"/>.</summary>
    Task<DateTime?> GetRegularDayAsync(DateTime startDate, DateTime endDate, AbsenceType absenceType, Employee employee);

    /// <summary>Ported from legacy <c>TimeDataService.GetTotalHoursInDay</c>.</summary>
    Task<double> GetTotalHoursInDayAsync(Employee employee, DateTime dayDate);
}
