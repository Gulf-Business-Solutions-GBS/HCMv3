using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeAbsenceCalculationService"/>
/// <remarks>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAbsence</c>'s
/// static calculation-engine region (legacy lines ~3388-3800, plus the <c>RecalculateTotalLeaveDays</c> hook
/// at line 2714) -- "Group F", a piece of legacy business logic previously deferred as out-of-scope for a
/// quick cleanup and now ported properly on its own. This is a static-methods-only legacy region operating
/// on an XPO <c>Session</c>, ported here as an <see cref="Abp.Domain.Services.IDomainService"/> with
/// injected <c>IRepository&lt;T&gt;</c> instances and the already-ported <see cref="ITimeDataService"/>/
/// <see cref="IRosterService"/>/<see cref="ISysSettingsAccessor"/> companions.
///
/// <para><b>Deliberate adaptations, all documented at the point of use below:</b></para>
/// <list type="bullet">
/// <item><description><b>Dead "duty not found / weekly-off / rest-day" validation block in
/// <c>GetHoulyLeaveDay</c></b> (legacy lines 3640-3668): every branch either called the UI-only
/// <c>ErrorHandling.Instance.SetPageError</c> singleton (when <c>OnlyMessage</c> was true, no ported
/// equivalent exists for a domain-service-layer "toast a page error" concept) or constructed-but-never-threw
/// a bare <c>new Exception(msg)</c> (when <c>OnlyMessage</c> was false) -- neither branch affects control
/// flow or the returned value in the legacy source either way. Faithfully dropped as genuinely dead code
/// (not a simplification of live logic); the <c>AbsenceType</c>/<c>OnlyMessage</c> parameters that only fed
/// this dead block are dropped from the signature accordingly. See
/// <see cref="IEmployeeAbsenceCalculationService.GetHoulyLeaveDayAsync"/> for full detail.</description></item>
/// <item><description><b>Duty.StartTime/EndTime "not set" sentinel</b>: legacy checked
/// <c>duty.StartTime == DateTimeExtension.DBDtNull || duty.EndTime == DateTimeExtension.DBDtNull</c> (an XPO
/// DateTime sentinel meaning "not configured") to decide whether to fall back to
/// <c>ManagePayrollSettings.DefaultWorkingHours</c>. <see cref="Duty.StartTime"/>/<see cref="Duty.EndTime"/>
/// are ported as non-nullable <see cref="TimeSpan"/> (see that entity's own class remarks), so there is no
/// literal sentinel value available. This port treats <em>both</em> being exactly <see cref="TimeSpan.Zero"/>
/// (a Duty configured with no shift times at all) as the equivalent "not configured" condition and falls
/// back to <c>DefaultWorkingHours</c> exactly as legacy did -- the most faithful reconstruction possible
/// without the original sentinel.</description></item>
/// <item><description><b>Null-safety on <c>CheckForFullMonthLeave</c></b>: legacy's
/// <c>(FullMonthLeaveAdjForFixedDays == true &amp;&amp; (AbsType.Code == "1" || ...))</c> dereferences
/// <c>AbsType</c> unconditionally, even though the same method treats <c>AbsType == null</c> as a valid input
/// a few lines later for the exclusion-list branch -- a latent legacy NullReferenceException bug for a
/// (never actually exercised, per every real call site in this codebase) null AbsenceType. This port adds an
/// explicit <c>absenceType != null &amp;&amp;</c> guard ahead of the Code/QuotaType checks; behavior is
/// identical for every real caller (all of which always supply a concrete AbsenceType).</description></item>
/// </list>
///
/// <see cref="UnitOfWorkAttribute"/> at the class level, matching every other domain service in this port
/// (<c>AccrualService</c>/<c>TimeDataService</c>/<c>RosterService</c>) so this service is independently
/// callable, opening its own unit of work when not already invoked from within one.
/// </remarks>
[UnitOfWork]
public class EmployeeAbsenceCalculationService : HCMv3DomainServiceBase, IEmployeeAbsenceCalculationService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _employeeSubstituteDutyRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<DutyPeriod> _dutyPeriodRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRepository<BreakShift> _breakShiftRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly ITimeDataService _timeDataService;
    private readonly IRosterService _rosterService;

    public EmployeeAbsenceCalculationService(
        IRepository<Employee> employeeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeSubstituteDuty> employeeSubstituteDutyRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<DutyPeriod> dutyPeriodRepository,
        IRepository<Duty> dutyRepository,
        IRepository<BreakShift> breakShiftRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        ITimeDataService timeDataService,
        IRosterService rosterService)
    {
        _employeeRepository = employeeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeSubstituteDutyRepository = employeeSubstituteDutyRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _dutyPeriodRepository = dutyPeriodRepository;
        _dutyRepository = dutyRepository;
        _breakShiftRepository = breakShiftRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _timeDataService = timeDataService;
        _rosterService = rosterService;
    }

    // NOTE: HCMv3.Core has no reference to EntityFrameworkCore (per architecture rules), so the usual
    // Microsoft.EntityFrameworkCore async IQueryable extensions (.ToListAsync()/.FirstOrDefaultAsync()/etc.)
    // are not available here -- same documented constraint as SysSettingsAccessor/TimeDataService/RosterService/
    // AccrualService. All queries use plain synchronous System.Linq over IRepository<T>.GetAll().

    #region << GetRecoveryPeriods >>

    public List<DateTime> GetRecoveryPeriods(DateTime payDate, DateTime offCycleEndDate)
    {
        var recoveryPeriods = new List<DateTime>();
        var recoveryStartDate = new DateTime(payDate.Year, payDate.Month, 1, 0, 0, 0);

        while (recoveryStartDate <= offCycleEndDate)
        {
            var monthStart = new DateTime(recoveryStartDate.Year, recoveryStartDate.Month, 1, 0, 0, 0);
            if (!recoveryPeriods.Contains(monthStart))
            {
                recoveryPeriods.Add(monthStart);
            }

            var daysInMonth = DateTime.DaysInMonth(recoveryStartDate.Year, recoveryStartDate.Month);
            recoveryStartDate = new DateTime(recoveryStartDate.Year, recoveryStartDate.Month, daysInMonth, 0, 0, 0).AddDays(1);
        }

        return recoveryPeriods;
    }

    #endregion

    #region << ActualLeaveDays >>

    public async Task<decimal> ActualLeaveDaysAsync(int employeeId, DateTime startDate, DateTime endDate, int? absenceTypeId, List<EmployeeSubstituteDuty> empSubstituteDutyList = null)
    {
        var employee = GetEmployeeOrThrow(employeeId);
        var absenceType = absenceTypeId.HasValue ? GetAbsenceTypeOrThrow(absenceTypeId.Value) : null;

        decimal totalLeaveDays = 0;

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var fullMonthLeaveAdjForFixedDays = payrollSettings?.FullMonthLeaveAdjForFixedDays ?? false;
        var totalDays = (endDate - startDate).TotalDays + 1;
        // See class remarks: explicit null-guard added ahead of AbsType.Code/QuotaType dereference.
        var checkForFullMonthLeave = fullMonthLeaveAdjForFixedDays
            && absenceType != null
            && (absenceType.Code == "1" || absenceType.QuotaType == QuotaType.AnnualLeave)
            && totalDays > 27;

        empSubstituteDutyList ??= _employeeSubstituteDutyRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.SubstituteDate >= startDate && r.SubstituteDate <= endDate)
            .ToList();

        var empDutyPeriodList = _employeeDutyPeriodRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && !(r.StartDate.Date > endDate.Date || r.EndDate.Date < startDate.Date))
            .ToList();

        if (!checkForFullMonthLeave)
        {
            var (weekend, publicHoliday, restDay) = await GetExclusionListsAsync(employee, startDate, endDate, absenceType, empSubstituteDutyList, empDutyPeriodList);
            var uniqueDates = weekend.Concat(publicHoliday).Concat(restDay).Distinct().Count();
            totalLeaveDays += uniqueDates;
        }
        else
        {
            var recoveryPeriods = GetRecoveryPeriods(startDate, endDate);

            foreach (var rec in recoveryPeriods)
            {
                var daysInMonth = DateTime.DaysInMonth(rec.Year, rec.Month);
                var monthEndDateFull = new DateTime(rec.Year, rec.Month, daysInMonth);

                var monthStartDate = rec < startDate ? startDate : rec;
                var monthEndDate = monthEndDateFull < endDate ? monthEndDateFull : endDate;

                var (weekend, publicHoliday, restDay) = await GetExclusionListsAsync(employee, monthStartDate, monthEndDate, absenceType, empSubstituteDutyList, empDutyPeriodList);

                if (monthStartDate.Day == 1 && monthEndDate.Day == DateTime.DaysInMonth(monthEndDate.Year, monthEndDate.Month))
                {
                    // Full-month leave: 26 fixed working days, plus any public-holiday/rest-day exclusion
                    // (legacy deliberately does NOT also exclude weekend dates in this branch).
                    var dayExclude = daysInMonth - 26 + publicHoliday.Concat(restDay).Distinct().Count();
                    totalLeaveDays += dayExclude;
                }
                else
                {
                    var dayExclude = weekend.Concat(publicHoliday).Concat(restDay).Distinct().Count();
                    totalLeaveDays += dayExclude;
                }
            }
        }

        return totalLeaveDays;
    }

    private async Task<(List<DateTime> Weekend, List<DateTime> PublicHoliday, List<DateTime> RestDay)> GetExclusionListsAsync(
        Employee employee, DateTime startDate, DateTime endDate, AbsenceType absenceType,
        List<EmployeeSubstituteDuty> empSubstituteDutyList, List<EmployeeDutyPeriod> empDutyPeriodList)
    {
        List<DateTime> weekend;
        List<DateTime> publicHoliday;
        List<DateTime> restDay;

        if (absenceType != null)
        {
            weekend = !absenceType.IncludeWeekend
                ? await _timeDataService.GetWeeklyOffDatesListAsync(employee, startDate, endDate, empSubstituteDutyList, empDutyPeriodList)
                : new List<DateTime>();

            publicHoliday = !absenceType.IncludePublicHoliday
                ? await _timeDataService.GetPublicHolidaysDatesAsync(employee, startDate, endDate, empDutyPeriodList)
                : new List<DateTime>();

            restDay = !absenceType.IncludeRestDay
                ? await _timeDataService.GetRestDaysListAsync(employee, startDate, endDate, empSubstituteDutyList, empDutyPeriodList)
                : new List<DateTime>();
        }
        else
        {
            weekend = await _timeDataService.GetWeeklyOffDatesListAsync(employee, startDate, endDate, empSubstituteDutyList, empDutyPeriodList);
            publicHoliday = await _timeDataService.GetPublicHolidaysDatesAsync(employee, startDate, endDate, empDutyPeriodList);
            restDay = await _timeDataService.GetRestDaysListAsync(employee, startDate, endDate, empSubstituteDutyList, empDutyPeriodList);
        }

        return (weekend, publicHoliday, restDay);
    }

    #endregion

    #region << HourlyBased Leave Function >>

    public async Task<double> GetHoulyLeaveDayAsync(int employeeId, DateTime leaveDayDate, double absenceHours)
    {
        double leaveDay = 0;
        var absenceMinutes = absenceHours * 60;

        // Throws if not resolvable, matching legacy GetEmployeeDuty's own throw-on-null behavior.
        var duty = await GetEmployeeDutyAsync(employeeId, leaveDayDate);

        double plannedMinutes;
        if (duty.StartTime == TimeSpan.Zero && duty.EndTime == TimeSpan.Zero)
        {
            // See class remarks: TimeSpan.Zero/TimeSpan.Zero stands in for legacy's DBDtNull "not configured" sentinel.
            var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
            plannedMinutes = (payrollSettings?.DefaultWorkingHours ?? 0) * 60;
        }
        else
        {
            var shiftStart = leaveDayDate.Date + duty.StartTime;
            var shiftEnd = duty.IsNightShift ? leaveDayDate.Date.AddDays(1) + duty.EndTime : leaveDayDate.Date + duty.EndTime;

            if (duty.BreakShiftId == null)
            {
                plannedMinutes = (shiftEnd - shiftStart).TotalMinutes;
            }
            else
            {
                var breakShift = _breakShiftRepository.GetAll().FirstOrDefault(b => b.Id == duty.BreakShiftId.Value);

                var breakStart = breakShift.IsBreakStartNextDay
                    ? leaveDayDate.Date.AddDays(1) + breakShift.StartTime
                    : leaveDayDate.Date + breakShift.StartTime;

                var breakEnd = (breakShift.IsBreakStartNextDay || breakShift.IsBreakEndNextDay)
                    ? leaveDayDate.Date.AddDays(1) + breakShift.EndTime
                    : leaveDayDate.Date + breakShift.EndTime;

                plannedMinutes = (breakStart - shiftStart).TotalMinutes;
                plannedMinutes += (shiftEnd - breakEnd).TotalMinutes;
            }

            if (plannedMinutes < 0)
            {
                plannedMinutes = 0;
            }
        }

        if (plannedMinutes > 0)
        {
            leaveDay = Math.Round(absenceMinutes / plannedMinutes, 3);
        }

        return leaveDay;
    }

    public async Task<Duty> GetEmployeeDutyAsync(int employeeId, DateTime leaveDayDate)
    {
        var employee = GetEmployeeOrThrow(employeeId);

        var duty = await TryGetEmployeeDutyAsync(employeeId, leaveDayDate);

        if (duty == null)
        {
            throw new InvalidOperationException($"Duty not found for Employee '{employee.Code}' for Date '{leaveDayDate:dd/MM/yyyy}'.");
        }

        return duty;
    }

    /// <summary>
    /// The duty resolution of <see cref="GetEmployeeDutyAsync"/> WITHOUT its throw-on-null ending -- legacy's
    /// <c>EmployeeDutyPeriod.GetDuty</c>, which simply returned null and whose callers tested for it
    /// (<c>duty != null &amp;&amp; duty.AllowTransEnd == false</c>). The date projectors below need that
    /// tolerance: a day with no duty configured is a day they walk past, not a day that fails the request.
    /// </summary>
    private async Task<Duty> TryGetEmployeeDutyAsync(int employeeId, DateTime leaveDayDate)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();

        Duty duty;

        if (timeSettings != null && timeSettings.EnableRosterDuty)
        {
            duty = await _rosterService.GetRosterDutyAsync(employeeId, leaveDayDate);
        }
        else
        {
            var substituteDutyId = _employeeSubstituteDutyRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.SubstituteDate == leaveDayDate)
                .Select(r => (int?)r.DutyId)
                .FirstOrDefault();

            if (substituteDutyId != null)
            {
                duty = _dutyRepository.GetAll().FirstOrDefault(d => d.Id == substituteDutyId.Value);
            }
            else
            {
                var dutyPeriod = await TryGetEmployeeDutyPeriodAsync(employeeId, leaveDayDate);
                if (dutyPeriod == null)
                {
                    return null;
                }

                var dutyId = leaveDayDate.DayOfWeek switch
                {
                    DayOfWeek.Sunday => dutyPeriod.SundayDutyId,
                    DayOfWeek.Monday => dutyPeriod.MondayDutyId,
                    DayOfWeek.Tuesday => dutyPeriod.TuesdayDutyId,
                    DayOfWeek.Wednesday => dutyPeriod.WednesdayDutyId,
                    DayOfWeek.Thursday => dutyPeriod.ThursdayDutyId,
                    DayOfWeek.Friday => dutyPeriod.FridayDutyId,
                    _ => dutyPeriod.SaturdayDutyId
                };

                duty = dutyId == null ? null : _dutyRepository.GetAll().FirstOrDefault(d => d.Id == dutyId.Value);
            }
        }

        return duty;
    }

    public async Task<DutyPeriod> GetEmployeeDutyPeriodAsync(int employeeId, DateTime leaveDayDate)
    {
        var employee = GetEmployeeOrThrow(employeeId);

        var dutyPeriod = await TryGetEmployeeDutyPeriodAsync(employeeId, leaveDayDate);

        if (dutyPeriod == null)
        {
            throw new InvalidOperationException($"Duty Period not found for Employee '{employee.Code}' for Date '{leaveDayDate:dd/MM/yyyy}'.");
        }

        return dutyPeriod;
    }

    /// <summary>Null-tolerant counterpart of <see cref="GetEmployeeDutyPeriodAsync"/>; see <see cref="TryGetEmployeeDutyAsync"/>.</summary>
    private async Task<DutyPeriod> TryGetEmployeeDutyPeriodAsync(int employeeId, DateTime leaveDayDate)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();

        DutyPeriod dutyPeriod;

        if (timeSettings != null && timeSettings.EnableRosterDuty)
        {
            dutyPeriod = await _rosterService.GetRosterDutyPeriodAsync(employeeId, leaveDayDate);
        }
        else
        {
            var dutyPeriodId = _employeeDutyPeriodRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.StartDate <= leaveDayDate && r.EndDate >= leaveDayDate)
                .Select(r => (int?)r.DutyPeriodId)
                .FirstOrDefault();

            dutyPeriod = dutyPeriodId == null ? null : _dutyPeriodRepository.GetAll().FirstOrDefault(dp => dp.Id == dutyPeriodId.Value);
        }

        return dutyPeriod;
    }

    #endregion

    #region << CalculateTotalLeaveDays >>

    public async Task<double> CalculateTotalLeaveDaysAsync(int employeeId, int absenceTypeId, DateTime startDate, DateTime endDate, double absenceHours)
    {
        // Legacy's `if (IsLoading || IsSaving) return;` XPO in-memory-state guard has no equivalent/purpose
        // in this on-demand service method form (see interface remarks) -- not ported.
        if (startDate == default || endDate == default || employeeId <= 0 || absenceTypeId <= 0)
        {
            return 0;
        }

        double result;

        if (absenceHours == 0)
        {
            // Full-day leave.
            var holidays = (double)await ActualLeaveDaysAsync(employeeId, startDate, endDate, absenceTypeId);
            result = (endDate.Subtract(startDate).Days + 1) - holidays;
        }
        else
        {
            // Hourly leave.
            var totalDays = await GetHoulyLeaveDayAsync(employeeId, startDate, absenceHours);
            var holidays = (double)await ActualLeaveDaysAsync(employeeId, startDate, endDate, absenceTypeId);
            result = totalDays - holidays;
            if (result < 0)
            {
                result = 0;
            }
        }

        return result;
    }

    public Task<double> CalculateTotalLeaveDaysAsync(EmployeeAbsence employeeAbsence)
    {
        if (employeeAbsence == null)
        {
            throw new ArgumentNullException(nameof(employeeAbsence));
        }

        return CalculateTotalLeaveDaysAsync(employeeAbsence.EmployeeId, employeeAbsence.AbsenceTypeId, employeeAbsence.StartDate, employeeAbsence.EndDate, employeeAbsence.AbsenceHours);
    }

    #endregion

    #region << Date projectors: CalcLeaveEndDate / GetRegularDate >>

    /// <summary>
    /// Hard stop on <see cref="CalcLeaveEndDateAsync"/>'s walk. Legacy's loop had no bound at all, so a
    /// calendar in which no day is ever countable -- a duty that forbids ending on a weekly-off combined with
    /// a full-week weekend, say -- span forever and took the request with it. Ten years is far past any
    /// leave anybody books, so hitting it means the data is wrong, and the honest answer is "no such date".
    /// </summary>
    private const int MaxProjectionDays = 3650;

    public async Task<DateTime?> CalcLeaveEndDateAsync(int employeeId, int? absenceTypeId, DateTime endDate, double quotaDays, int excludeEmployeeAbsenceId = 0)
    {
        var employee = GetEmployeeOrThrow(employeeId);
        var absenceType = absenceTypeId.HasValue ? GetAbsenceTypeOrThrow(absenceTypeId.Value) : null;

        var counted = 0;
        DateTime? foundDate = null;
        var candidate = endDate.Date.AddDays(1);
        var walked = 0;

        while (counted < quotaDays)
        {
            if (walked++ > MaxProjectionDays)
            {
                return null;
            }

            var duty = await TryGetEmployeeDutyAsync(employeeId, candidate);

            if (await _timeDataService.IsHolidayAsync(candidate, absenceType, employee))
            {
                // A public holiday is never countable, whatever the duty says -- legacy checks it first and
                // without the AllowTransEnd escape the other two branches have.
                foundDate = null;
            }
            else if (await _timeDataService.IsWeekEndAsync(candidate, absenceType, employee) && duty != null && !duty.AllowTransEnd)
            {
                foundDate = null;
            }
            else if (await _timeDataService.IsRestDayAsync(candidate, absenceType, employee) && duty != null && !duty.AllowTransEnd)
            {
                foundDate = null;
            }
            else if (LeaveQuotaOnDay(employeeId, candidate, excludeEmployeeAbsenceId) > 0)
            {
                // Another absence already starts here, so this day is spoken for.
                foundDate = null;
            }
            else
            {
                foundDate = candidate;
                counted++;
            }

            candidate = candidate.AddDays(1);
        }

        return foundDate;
    }

    public Task<DateTime?> GetRegularDateAsync(int employeeId, DateTime rejoinDate, int excludeEmployeeAbsenceId = 0)
    {
        var lastDate = rejoinDate.Date.AddDays(-1);

        if (LeaveQuotaOnDay(employeeId, lastDate, excludeEmployeeAbsenceId) > 0)
        {
            return Task.FromResult<DateTime?>(null);
        }

        return Task.FromResult<DateTime?>(lastDate);
    }

    /// <summary>
    /// Ported from legacy <c>EmployeeAbsence.LeaveQuota(Session, StartDate, EndDate, Employee, _Oid)</c>,
    /// narrowed to the single-day window every live caller passes: the leave days belonging to absences of
    /// this employee that START on <paramref name="day"/>.
    ///
    /// <para>
    /// Legacy's predicate was <c>((StartDate &lt;= r.StartDate &amp;&amp; r.StartDate &lt;= EndDate) ||
    /// (r.StartDate &gt;= StartDate &amp;&amp; r.StartDate &lt;= EndDate))</c> -- two spellings of the same
    /// condition OR'd together, so only one is kept here. Note it tests the START date only: a long leave
    /// running THROUGH the day does not register. That is legacy behaviour and is left alone, since this
    /// exists to catch a second leave beginning where the first would end, and the overlap check proper
    /// lives in <c>LeaveWorkflowTransactionHandler</c>.
    /// </para>
    /// </summary>
    private double LeaveQuotaOnDay(int employeeId, DateTime day, int excludeEmployeeAbsenceId)
    {
        var date = day.Date;

        return _employeeAbsenceRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && r.Id != excludeEmployeeAbsenceId
                        && r.StartDate >= date
                        && r.StartDate < date.AddDays(1))
            .Sum(r => (double?)r.TotalLeaveDays) ?? 0;
    }

    #endregion

    private Employee GetEmployeeOrThrow(int employeeId)
    {
        return _employeeRepository.GetAll().FirstOrDefault(r => r.Id == employeeId)
               ?? throw new InvalidOperationException($"Employee Id '{employeeId}' not found.");
    }

    private AbsenceType GetAbsenceTypeOrThrow(int absenceTypeId)
    {
        return _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Id == absenceTypeId)
               ?? throw new InvalidOperationException($"AbsenceType Id '{absenceTypeId}' not found.");
    }
}
