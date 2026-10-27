using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;

namespace HCMv3.TimeManagement.Services;

/// <inheritdoc cref="ITimeDataService"/>
/// <remarks>
/// <see cref="UnitOfWorkAttribute"/> at the class level, same reason/precedent as
/// <c>HCMv3.SystemSettings.Services.SysSettingsAccessor</c> (Wave 1): so this domain service is
/// independently callable, opening its own unit of work/DbContext when not already invoked from within one.
///
/// NOTE ON ASYNC: <c>HCMv3.Core</c> has no reference to EntityFrameworkCore (per architecture rules), so the
/// usual <c>Microsoft.EntityFrameworkCore</c> async <c>IQueryable</c> extensions (<c>.ToListAsync()</c>,
/// <c>.FirstOrDefaultAsync()</c>, <c>.Include()</c>) are not available here -- same constraint already
/// documented in <c>SysSettingsAccessor</c>. All queries in this class therefore use plain synchronous
/// <c>System.Linq</c> methods against the <c>IQueryable</c> returned by <c>IRepository&lt;T&gt;.GetAll()</c>,
/// with results still exposed via the (interface-mandated) <c>Task&lt;T&gt;</c>-returning public methods by
/// wrapping the synchronously-computed result in <c>Task.FromResult</c>.
///
/// NAV-PROPERTY LOADING: since <c>.Include()</c> is unavailable, related data is fetched either via
/// dedicated single-purpose queries (e.g. <see cref="GetDutyInfoById"/>) or via <c>.Select()</c> projections
/// that dot into a navigation path (e.g. <c>h.Holiday.HolidayCalendarId</c>) -- EF Core translates these into
/// SQL joins without requiring <c>Include</c>, exactly like the legacy source's own anonymous-object Select
/// projections in <c>GetPublicHolidaysdatesList</c>.
/// </remarks>
[UnitOfWork]
public class TimeDataService : HCMv3DomainServiceBase, ITimeDataService
{
    private readonly IRepository<HolidayPeriod> _holidayPeriodRepository;
    private readonly IRepository<DutyPeriod> _dutyPeriodRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _employeeSubstituteDutyRepository;
    private readonly IRepository<RosterGroupDetail> _rosterGroupDetailRepository;
    private readonly IRepository<RosterGroupSchedule> _rosterGroupScheduleRepository;
    private readonly IRepository<ManageTimeSettings> _timeSettingsRepository;

    public TimeDataService(
        IRepository<HolidayPeriod> holidayPeriodRepository,
        IRepository<DutyPeriod> dutyPeriodRepository,
        IRepository<Duty> dutyRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeSubstituteDuty> employeeSubstituteDutyRepository,
        IRepository<RosterGroupDetail> rosterGroupDetailRepository,
        IRepository<RosterGroupSchedule> rosterGroupScheduleRepository,
        IRepository<ManageTimeSettings> timeSettingsRepository)
    {
        _holidayPeriodRepository = holidayPeriodRepository;
        _dutyPeriodRepository = dutyPeriodRepository;
        _dutyRepository = dutyRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeSubstituteDutyRepository = employeeSubstituteDutyRepository;
        _rosterGroupDetailRepository = rosterGroupDetailRepository;
        _rosterGroupScheduleRepository = rosterGroupScheduleRepository;
        _timeSettingsRepository = timeSettingsRepository;
    }

    /// <summary>Flattened projection of the Duty fields this engine needs (see class remarks on why full
    /// entity graphs aren't loaded via Include).</summary>
    private sealed class DutyInfo
    {
        public bool IsWeeklyOff { get; set; }
        public bool IsRestDay { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsNightShift { get; set; }
        public bool HasBreakShift { get; set; }
        public TimeSpan BreakStartTime { get; set; }
        public TimeSpan BreakEndTime { get; set; }
        public bool BreakIsStartNextDay { get; set; }
        public bool BreakIsEndNextDay { get; set; }
    }

    public Task<bool> IsHolidayAsync(DateTime dayDate, AbsenceType absenceType, Employee employee)
    {
        if (absenceType != null && absenceType.IncludePublicHoliday)
        {
            return Task.FromResult(false);
        }

        var dates = GetPublicHolidaysDatesInternal(employee, dayDate.Date, dayDate.Date, null);
        return Task.FromResult(dates.Any(d => d.Date == dayDate.Date));
    }

    public Task<List<DateTime>> GetPublicHolidaysDatesAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeDutyPeriod> empDutyPeriodList = null)
    {
        return Task.FromResult(GetPublicHolidaysDatesInternal(employee, startDate, endDate, empDutyPeriodList));
    }

    public Task<List<(DateTime HolidayDate, string HolidayName)>> GetPublicHolidaysDatesListAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var holidayPeriods = _holidayPeriodRepository.GetAll()
            .Where(h => h.HolidayDate >= startDate.Date && h.HolidayDate <= endDate.Date)
            .Select(h => new { h.HolidayDate, HolidayCalendarId = h.Holiday.HolidayCalendarId, HolidayName = h.Holiday.NameEn })
            .OrderBy(h => h.HolidayDate)
            .ToList();

        var dates = new List<(DateTime, string)>();
        if (holidayPeriods.Count == 0)
        {
            return Task.FromResult(dates);
        }

        var holidayDates = holidayPeriods.Select(h => h.HolidayDate).Distinct().ToList();
        var timeSettings = _timeSettingsRepository.GetAll().FirstOrDefault();
        var enableRoster = timeSettings?.EnableRosterDuty ?? false;

        List<EmployeeDutyPeriod> empDutyPeriodList = null;
        if (!enableRoster)
        {
            empDutyPeriodList = LoadDutyPeriodRange(employeeId, startDate, endDate);
        }

        foreach (var dayDate in holidayDates)
        {
            var dutyPeriodId = ResolveDutyPeriodId(enableRoster, employeeId, dayDate, empDutyPeriodList);
            if (dutyPeriodId == null)
            {
                continue;
            }

            var holidayCalendarId = GetDutyPeriodHolidayCalendarId(dutyPeriodId.Value);
            if (holidayCalendarId == null)
            {
                continue;
            }

            var match = holidayPeriods.FirstOrDefault(h => h.HolidayCalendarId == holidayCalendarId.Value && h.HolidayDate == dayDate);
            if (match != null)
            {
                dates.Add((dayDate, match.HolidayName));
            }
        }

        if (timeSettings?.CountWeekoffOnPH == true)
        {
            dates = dates.Where(d =>
            {
                var duty = GetDutyForDay(enableRoster, employeeId, d.Item1, null, empDutyPeriodList, throwIfMissing: false, employeeCode: null);
                return duty == null || !duty.IsWeeklyOff;
            }).ToList();
        }

        if (timeSettings?.CountRestDayOnPH == true)
        {
            dates = dates.Where(d =>
            {
                var duty = GetDutyForDay(enableRoster, employeeId, d.Item1, null, empDutyPeriodList, throwIfMissing: false, employeeCode: null);
                return duty == null || !duty.IsRestDay;
            }).ToList();
        }

        return Task.FromResult(dates);
    }

    public Task<bool> IsWeekEndAsync(DateTime dayDate, AbsenceType absenceType, Employee employee)
    {
        if (absenceType != null && absenceType.IncludeWeekend)
        {
            return Task.FromResult(false);
        }

        var dates = GetFlaggedDates(employee, dayDate.Date, dayDate.Date, null, null, d => d.IsWeeklyOff);
        return Task.FromResult(dates.Any(d => d.Date == dayDate.Date));
    }

    public Task<List<DateTime>> GetWeeklyOffDatesListAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, List<EmployeeDutyPeriod> empDutyPeriodList = null)
    {
        return Task.FromResult(GetFlaggedDates(employee, startDate, endDate, empSubstituteDutyList, empDutyPeriodList, d => d.IsWeeklyOff));
    }

    public async Task<double> GetWeeklyOffDaysCountAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, List<EmployeeDutyPeriod> empDutyPeriodList = null)
    {
        var dates = await GetWeeklyOffDatesListAsync(employee, startDate, endDate, empSubstituteDutyList, empDutyPeriodList);
        return dates.Count;
    }

    public Task<bool> IsRestDayAsync(DateTime dayDate, AbsenceType absenceType, Employee employee)
    {
        if (absenceType != null && absenceType.IncludeRestDay)
        {
            return Task.FromResult(false);
        }

        var dates = GetFlaggedDates(employee, dayDate.Date, dayDate.Date, null, null, d => d.IsRestDay);
        return Task.FromResult(dates.Any(d => d.Date == dayDate.Date));
    }

    public Task<List<DateTime>> GetRestDaysListAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, List<EmployeeDutyPeriod> empDutyPeriodList = null)
    {
        return Task.FromResult(GetFlaggedDates(employee, startDate, endDate, empSubstituteDutyList, empDutyPeriodList, d => d.IsRestDay));
    }

    public async Task<double> GetRestDaysCountAsync(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, List<EmployeeDutyPeriod> empDutyPeriodList = null)
    {
        var dates = await GetRestDaysListAsync(employee, startDate, endDate, empSubstituteDutyList, empDutyPeriodList);
        return dates.Count;
    }

    public async Task<DateTime?> GetRegularDayAsync(DateTime startDate, DateTime endDate, AbsenceType absenceType, Employee employee)
    {
        for (var date = startDate; date < endDate; date = date.AddDays(1))
        {
            var isWeekOff = await IsWeekEndAsync(date, absenceType, employee);
            var isHoliday = await IsHolidayAsync(date, absenceType, employee);
            var isRestDay = await IsRestDayAsync(date, absenceType, employee);

            if (!isWeekOff && !isHoliday && !isRestDay)
            {
                return date;
            }
        }

        return null;
    }

    public Task<double> GetTotalHoursInDayAsync(Employee employee, DateTime dayDate)
    {
        var timeSettings = _timeSettingsRepository.GetAll().FirstOrDefault();
        var enableRoster = timeSettings?.EnableRosterDuty ?? false;

        var duty = GetDutyForDay(enableRoster, employee.Id, dayDate, null, null, throwIfMissing: false, employee.Code);
        if (duty == null)
        {
            return Task.FromResult(0d);
        }

        var shiftStart = dayDate.Date + duty.StartTime;
        var shiftEnd = duty.IsNightShift ? dayDate.Date.AddDays(1) + duty.EndTime : dayDate.Date + duty.EndTime;

        double totalMinutes;
        if (!duty.HasBreakShift)
        {
            totalMinutes = (shiftEnd - shiftStart).TotalMinutes;
        }
        else
        {
            var breakStart = duty.BreakIsStartNextDay
                ? dayDate.Date.AddDays(1) + duty.BreakStartTime
                : dayDate.Date + duty.BreakStartTime;

            var breakEnd = (duty.BreakIsStartNextDay || duty.BreakIsEndNextDay)
                ? dayDate.Date.AddDays(1) + duty.BreakEndTime
                : dayDate.Date + duty.BreakEndTime;

            totalMinutes = (breakStart - shiftStart).TotalMinutes + (shiftEnd - breakEnd).TotalMinutes;
        }

        return Task.FromResult(Math.Round(totalMinutes / 60, 3));
    }

    private List<DateTime> GetPublicHolidaysDatesInternal(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeDutyPeriod> empDutyPeriodList)
    {
        var holidayPeriods = _holidayPeriodRepository.GetAll()
            .Where(h => h.HolidayDate >= startDate.Date && h.HolidayDate <= endDate.Date)
            .Select(h => new { h.HolidayDate, HolidayCalendarId = h.Holiday.HolidayCalendarId })
            .ToList();

        var dates = new List<DateTime>();
        if (holidayPeriods.Count == 0)
        {
            return dates;
        }

        var holidayDates = holidayPeriods.Select(h => h.HolidayDate).Distinct().ToList();
        var timeSettings = _timeSettingsRepository.GetAll().FirstOrDefault();
        var enableRoster = timeSettings?.EnableRosterDuty ?? false;

        if (!enableRoster)
        {
            empDutyPeriodList ??= LoadDutyPeriodRange(employee.Id, startDate, endDate);
        }

        foreach (var dayDate in holidayDates)
        {
            var dutyPeriodId = ResolveDutyPeriodId(enableRoster, employee.Id, dayDate, empDutyPeriodList);
            if (dutyPeriodId == null)
            {
                throw new Exception($"Duty Period not found for Employee : '{employee.Code}' on Date : '{dayDate:dd/MM/yyyy}'");
            }

            var holidayCalendarId = GetDutyPeriodHolidayCalendarId(dutyPeriodId.Value);
            if (holidayCalendarId == null)
            {
                throw new Exception($"Holiday Calendar not found for Duty Period : {dutyPeriodId.Value}");
            }

            if (holidayPeriods.Any(h => h.HolidayCalendarId == holidayCalendarId.Value && h.HolidayDate == dayDate))
            {
                dates.Add(dayDate);
            }
        }

        if (timeSettings?.CountWeekoffOnPH == true)
        {
            dates = dates.Where(d =>
            {
                var duty = GetDutyForDay(enableRoster, employee.Id, d, null, empDutyPeriodList, throwIfMissing: false, employee.Code);
                return duty == null || !duty.IsWeeklyOff;
            }).ToList();
        }

        if (timeSettings?.CountRestDayOnPH == true)
        {
            dates = dates.Where(d =>
            {
                var duty = GetDutyForDay(enableRoster, employee.Id, d, null, empDutyPeriodList, throwIfMissing: false, employee.Code);
                return duty == null || !duty.IsRestDay;
            }).ToList();
        }

        return dates;
    }

    private List<DateTime> GetFlaggedDates(Employee employee, DateTime startDate, DateTime endDate, List<EmployeeSubstituteDuty> empSubstituteDutyList, List<EmployeeDutyPeriod> empDutyPeriodList, Func<DutyInfo, bool> predicate)
    {
        empSubstituteDutyList ??= LoadSubstituteDutyRange(employee.Id, startDate, endDate);

        var timeSettings = _timeSettingsRepository.GetAll().FirstOrDefault();
        var enableRoster = timeSettings?.EnableRosterDuty ?? false;

        if (!enableRoster)
        {
            empDutyPeriodList ??= LoadDutyPeriodRange(employee.Id, startDate, endDate);
        }

        var dates = new List<DateTime>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var duty = GetDutyForDay(enableRoster, employee.Id, date, empSubstituteDutyList, empDutyPeriodList, throwIfMissing: true, employee.Code);
            if (duty != null && predicate(duty))
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    private List<EmployeeSubstituteDuty> LoadSubstituteDutyRange(int employeeId, DateTime startDate, DateTime endDate)
    {
        return _employeeSubstituteDutyRepository.GetAll()
            .Where(s => s.EmployeeId == employeeId && s.SubstituteDate >= startDate && s.SubstituteDate <= endDate)
            .ToList();
    }

    private List<EmployeeDutyPeriod> LoadDutyPeriodRange(int employeeId, DateTime startDate, DateTime endDate)
    {
        return _employeeDutyPeriodRepository.GetAll()
            .Where(x => x.EmployeeId == employeeId && !(x.StartDate.Date > endDate.Date || x.EndDate.Date < startDate.Date))
            .ToList();
    }

    /// <summary>
    /// Resolves the effective Duty for an Employee on a date: substitute duty first (cache-list-aware,
    /// same as legacy), else the resolved DutyPeriod's day-of-week Duty (Roster or classic Weekly, per
    /// <paramref name="enableRoster"/>). <paramref name="throwIfMissing"/> mirrors the legacy distinction
    /// between call-sites that throw when no DutyPeriod can be resolved (Count/List methods) and the ones
    /// that silently treat it as "no duty" (the CountWeekoffOnPH/CountRestDayOnPH holiday filter, and
    /// GetTotalHoursInDay, both of which used the non-throwing 2-arg legacy <c>EmployeeDutyPeriod.GetDuty</c>
    /// overload).
    /// </summary>
    private DutyInfo GetDutyForDay(bool enableRoster, int employeeId, DateTime date, List<EmployeeSubstituteDuty> substituteList, List<EmployeeDutyPeriod> dutyPeriodList, bool throwIfMissing, string employeeCode)
    {
        int? dutyId;

        if (substituteList != null)
        {
            dutyId = substituteList
                .Where(s => s.EmployeeId == employeeId && s.SubstituteDate == date)
                .Select(s => (int?)s.DutyId)
                .FirstOrDefault();
        }
        else
        {
            dutyId = _employeeSubstituteDutyRepository.GetAll()
                .Where(s => s.EmployeeId == employeeId && s.SubstituteDate == date)
                .Select(s => (int?)s.DutyId)
                .FirstOrDefault();
        }

        if (dutyId != null)
        {
            return GetDutyInfoById(dutyId.Value);
        }

        var dutyPeriodId = ResolveDutyPeriodId(enableRoster, employeeId, date, dutyPeriodList);
        if (dutyPeriodId == null)
        {
            if (throwIfMissing)
            {
                throw new Exception($"Employee Duty Period not found for Employee : '{employeeCode}' on Date : '{date:dd/MM/yyyy}'");
            }

            return null;
        }

        return GetDayOfWeekDutyInfo(dutyPeriodId.Value, date.DayOfWeek);
    }

    private int? ResolveDutyPeriodId(bool enableRoster, int employeeId, DateTime date, List<EmployeeDutyPeriod> dutyPeriodList)
    {
        if (enableRoster)
        {
            return ResolveRosterDutyPeriodId(employeeId, date);
        }

        if (dutyPeriodList != null)
        {
            return dutyPeriodList
                .Where(x => x.EmployeeId == employeeId && x.StartDate <= date && x.EndDate >= date)
                .Select(x => (int?)x.DutyPeriodId)
                .FirstOrDefault();
        }

        return _employeeDutyPeriodRepository.GetAll()
            .Where(x => x.EmployeeId == employeeId && x.StartDate <= date && x.EndDate >= date)
            .Select(x => (int?)x.DutyPeriodId)
            .FirstOrDefault();
    }

    /// <summary>Reconstructed <c>RosterService.GetRosterDutyPeriod</c> equivalent -- see class remarks.</summary>
    private int? ResolveRosterDutyPeriodId(int employeeId, DateTime date)
    {
        var rosterGroupId = _rosterGroupDetailRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= date && r.EndDate >= date)
            .Select(r => (int?)r.RosterGroupId)
            .FirstOrDefault();

        if (rosterGroupId == null)
        {
            return null;
        }

        return _rosterGroupScheduleRepository.GetAll()
            .Where(r => r.RosterGroupId == rosterGroupId.Value && r.StartDate <= date && r.EndDate >= date)
            .Select(r => (int?)r.DutyPeriodId)
            .FirstOrDefault();
    }

    private int? GetDutyPeriodHolidayCalendarId(int dutyPeriodId)
    {
        return _dutyPeriodRepository.GetAll()
            .Where(dp => dp.Id == dutyPeriodId)
            .Select(dp => dp.HolidayCalendarId)
            .FirstOrDefault();
    }

    private DutyInfo GetDayOfWeekDutyInfo(int dutyPeriodId, DayOfWeek dayOfWeek)
    {
        var dutyId = _dutyPeriodRepository.GetAll()
            .Where(dp => dp.Id == dutyPeriodId)
            .Select(dp => dayOfWeek == DayOfWeek.Sunday ? dp.SundayDutyId
                        : dayOfWeek == DayOfWeek.Monday ? dp.MondayDutyId
                        : dayOfWeek == DayOfWeek.Tuesday ? dp.TuesdayDutyId
                        : dayOfWeek == DayOfWeek.Wednesday ? dp.WednesdayDutyId
                        : dayOfWeek == DayOfWeek.Thursday ? dp.ThursdayDutyId
                        : dayOfWeek == DayOfWeek.Friday ? dp.FridayDutyId
                        : dp.SaturdayDutyId)
            .FirstOrDefault();

        return dutyId == null ? null : GetDutyInfoById(dutyId.Value);
    }

    private DutyInfo GetDutyInfoById(int dutyId)
    {
        // NOTE: BreakShift is an OPTIONAL nav (nullable BreakShiftId). Projecting through it directly in a
        // single Select (e.g. "d.BreakShift.StartTime") makes EF Core generate a shaper that assumes the
        // joined row always exists, throwing "Nullable object must have a value" at runtime whenever
        // BreakShiftId is null. Resolved with a separate, explicit lookup instead.
        var duty = _dutyRepository.GetAll()
            .Where(d => d.Id == dutyId)
            .Select(d => new
            {
                d.IsWeeklyOff,
                d.IsRestDay,
                d.StartTime,
                d.EndTime,
                d.IsNightShift,
                d.BreakShiftId
            })
            .FirstOrDefault();

        if (duty == null)
        {
            return null;
        }

        var info = new DutyInfo
        {
            IsWeeklyOff = duty.IsWeeklyOff,
            IsRestDay = duty.IsRestDay,
            StartTime = duty.StartTime,
            EndTime = duty.EndTime,
            IsNightShift = duty.IsNightShift,
            HasBreakShift = duty.BreakShiftId != null
        };

        if (duty.BreakShiftId != null)
        {
            var breakShift = _dutyRepository.GetAll()
                .Where(d => d.Id == dutyId)
                .Select(d => new
                {
                    d.BreakShift.StartTime,
                    d.BreakShift.EndTime,
                    d.BreakShift.IsBreakStartNextDay,
                    d.BreakShift.IsBreakEndNextDay
                })
                .First();

            info.BreakStartTime = breakShift.StartTime;
            info.BreakEndTime = breakShift.EndTime;
            info.BreakIsStartNextDay = breakShift.IsBreakStartNextDay;
            info.BreakIsEndNextDay = breakShift.IsBreakEndNextDay;
        }

        return info;
    }
}
