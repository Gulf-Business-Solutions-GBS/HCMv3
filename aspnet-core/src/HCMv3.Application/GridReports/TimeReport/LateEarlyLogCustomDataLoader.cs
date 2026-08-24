using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// SHARED INPUT LOADER for the four customer-specific Late/Early Log variants ported in this wave --
/// <c>uscRptLateEarlyLog_Custom019 / _Custom023 / _Custom033 / _Custom036</c>.
///
/// WHY THIS EXISTS. The four legacy controls disagree sharply about what a row MEANS (see each app
/// service's remarks), but their "&lt;&lt; Load Initial Data &gt;&gt;" regions are the same six
/// window-bounded queries, character for character, and their "&lt;&lt; Get Duty &gt;&gt;" regions are the
/// same substitute-then-weekday-then-roster cascade. Copying ~250 lines of projection into four services
/// would give four places for the same query to drift. The DIFFERENCES between the four live entirely in
/// row shaping, which stays in the individual services where it can be read against its own legacy file.
///
/// It deliberately does NOT touch the already-ported base <see cref="LateEarlyLogReportAppService"/>,
/// which carries its own private copies. The base is a separate, shipped port; folding it in here would
/// mean editing a file this wave has already signed off.
///
/// WHAT VARIES between the four is expressed as options rather than as forks:
/// <list type="bullet">
/// <item><description><see cref="LateEarlyLogCustomLoadOptions.TimeEventLookbackDays"/> -- Custom019
/// widened the punch window one day BACKWARD (uscRptLateEarlyLog_Custom019.ascx.cs:926); the other three,
/// like the base, start at <c>dtFrom</c>.</description></item>
/// <item><description><see cref="LateEarlyLogCustomLoadOptions.FullDayAbsencesOnly"/> -- Custom033/036
/// filtered <c>AbsenceHours == 0</c> in the query itself (Custom033:719); the others load both full-day
/// and partial absences and separate them per row.</description></item>
/// </list>
///
/// Everything here is materialized in memory because the legacy row shaping depends on the day's resolved
/// Duty, the employee's TimePolicy limits and the night-shift/week-off flags -- none of which is
/// expressible as SQL. The window bounds keep that bounded.
/// </summary>
public class LateEarlyLogCustomDataLoader : ITransientDependency
{
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _substituteDutyRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRosterService _rosterService;
    private readonly ITimeDataService _timeDataService;

    public LateEarlyLogCustomDataLoader(
        IRepository<TimeEvent> timeEventRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSubstituteDuty> substituteDutyRepository,
        IRepository<Duty> dutyRepository,
        IRosterService rosterService,
        ITimeDataService timeDataService)
    {
        _timeEventRepository = timeEventRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _substituteDutyRepository = substituteDutyRepository;
        _dutyRepository = dutyRepository;
        _rosterService = rosterService;
        _timeDataService = timeDataService;
    }

    /// <summary>
    /// Runs the six window-bounded loads that every one of the four legacy controls ran, for the employee
    /// set the caller has ALREADY filtered and row-level-secured.
    /// </summary>
    public async Task<LateEarlyLogCustomDataSet> LoadAsync(
        IReadOnlyList<LateEarlyLogEmployeeInfo> employees,
        DateTime fromDate,
        DateTime toDate,
        bool useArabicNames,
        bool isRosterActive,
        LateEarlyLogCustomLoadOptions options = null)
    {
        options ??= new LateEarlyLogCustomLoadOptions();

        var employeeIds = employees.Select(e => e.Id).ToList();

        // Legacy: TransDate between dtFrom (optionally minus the lookback) and dtTo + 1 DAY. The trailing
        // day is what lets a night shift's clock-out, which falls on the following calendar day, be found.
        var timeEventFloor = fromDate.AddDays(-options.TimeEventLookbackDays);
        var timeEventCeiling = toDate.AddDays(1);

        var timeEvents = await _timeEventRepository.GetAll()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                        && t.TransDate.Date >= timeEventFloor
                        && t.TransDate.Date <= timeEventCeiling)
            .Select(t => new LateEarlyLogTimeEventInfo
            {
                EmployeeId = t.EmployeeId,
                TransTypeCode = t.TransType.Code,
                TransDate = t.TransDate,
                TransTime = t.TransTime,
                WorkLocation = t.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? t.WorkLocation.NameAr : t.WorkLocation.NameEn),
                AccessUnit = t.AccessUnit == null
                    ? string.Empty
                    : (useArabicNames ? t.AccessUnit.NameAr : t.AccessUnit.NameEn)
            })
            .ToListAsync();

        foreach (var timeEvent in timeEvents)
        {
            // Legacy MyTimeEvent.TransDateTime -- the punch DATE plus the punch TIME-OF-DAY.
            timeEvent.TransDateTime = timeEvent.TransDate.Date.Add(timeEvent.TransTime.TimeOfDay);
        }

        var dutyPeriodAssignments = await _employeeDutyPeriodRepository.GetAll()
            .Where(d => employeeIds.Contains(d.EmployeeId)
                        && d.StartDate.Date <= toDate
                        && d.EndDate.Date >= fromDate)
            .Select(d => new LateEarlyLogDutyPeriodInfo
            {
                EmployeeId = d.EmployeeId,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                SundayDutyId = d.DutyPeriod.SundayDutyId,
                MondayDutyId = d.DutyPeriod.MondayDutyId,
                TuesdayDutyId = d.DutyPeriod.TuesdayDutyId,
                WednesdayDutyId = d.DutyPeriod.WednesdayDutyId,
                ThursdayDutyId = d.DutyPeriod.ThursdayDutyId,
                FridayDutyId = d.DutyPeriod.FridayDutyId,
                SaturdayDutyId = d.DutyPeriod.SaturdayDutyId
            })
            .ToListAsync();

        var timePolicies = await _employeeTimePolicyRepository.GetAll()
            .Where(p => employeeIds.Contains(p.EmployeeId)
                        && p.StartDate.Date <= toDate
                        && p.EndDate.Date >= fromDate)
            .Select(p => new LateEarlyLogTimePolicyInfo
            {
                EmployeeId = p.EmployeeId,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                TimePolicyName = useArabicNames ? p.TimePolicy.NameAr : p.TimePolicy.NameEn,
                ClockInLimit = p.TimePolicy.ClockInLimit,
                ClockOutLimit = p.TimePolicy.ClockOutLimit,
                BreakInLimit = p.TimePolicy.BreakInLimit,
                BreakOutLimit = p.TimePolicy.BreakOutLimit,
                TimeEventRule = p.TimePolicy.TimeEventRule,
                TimeMgmtStatus = p.TimePolicy.TimeMgmtStatus,
                OTMinLimit = p.TimePolicy.OTMinLimit
            })
            .ToListAsync();

        var fullDayAbsencesOnly = options.FullDayAbsencesOnly;
        var absences = await _employeeAbsenceRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && a.StartDate.Date <= toDate
                        && a.EndDate.Date >= fromDate
                        && (!fullDayAbsencesOnly || a.AbsenceHours == 0))
            .Select(a => new LateEarlyLogAbsenceInfo
            {
                EmployeeId = a.EmployeeId,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                AbsenceTypeName = useArabicNames ? a.AbsenceType.NameAr : a.AbsenceType.NameEn,
                IsReturnDateRequired = a.AbsenceType.IsReturnDateRequired,
                IsPaid = a.AbsenceType.IsPaid,
                ReturnDate = a.ReturnDate,
                AbsenceHours = a.AbsenceHours
            })
            .ToListAsync();

        // Org assignments OVERLAPPING the window, not "as of today": the row's own day selects among them.
        var orgAssignments = await _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId)
                        && o.StartDate.Date <= toDate
                        && o.EndDate.Date >= fromDate)
            .Select(o => new LateEarlyLogOrgAssignmentInfo
            {
                EmployeeId = o.EmployeeId,
                StartDate = o.StartDate,
                EndDate = o.EndDate,
                PayOvertime = o.EmployeeSubGroup != null && o.EmployeeSubGroup.PayOvertime,
                PayrollCountry = o.PayrollArea == null || o.PayrollArea.Country == null
                    ? string.Empty
                    : (useArabicNames ? o.PayrollArea.Country.NameAr : o.PayrollArea.Country.NameEn),
                WorkLocation = o.WorkLocation == null
                    ? string.Empty
                    : (useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn),
                OrganizationUnitId = o.OrganizationUnitId
            })
            .OrderBy(o => o.StartDate)
            .ToListAsync();

        var substitutes = await _substituteDutyRepository.GetAll()
            .Where(s => employeeIds.Contains(s.EmployeeId)
                        && s.SubstituteDate.Date >= fromDate
                        && s.SubstituteDate.Date <= toDate)
            .Select(s => new { s.EmployeeId, s.SubstituteDate, s.DutyId })
            .ToListAsync();

        var duties = await _dutyRepository.GetAll()
            .Select(d => new LateEarlyLogDutyInfo
            {
                Id = d.Id,
                IsNightShift = d.IsNightShift,
                IsWeeklyOff = d.IsWeeklyOff,
                IsRestDay = d.IsRestDay,
                StartTime = d.StartTime,
                EndTime = d.EndTime,
                FlexiStartTime = d.FlexiStartTime,
                FlexiEndTime = d.FlexiEndTime,
                BreakShiftId = d.BreakShiftId,
                BreakStartTime = d.BreakShift == null ? (TimeSpan?)null : d.BreakShift.StartTime,
                BreakEndTime = d.BreakShift == null ? (TimeSpan?)null : d.BreakShift.EndTime,
                IsBreakStartNextDay = d.BreakShift != null && d.BreakShift.IsBreakStartNextDay,
                IsBreakEndNextDay = d.BreakShift != null && d.BreakShift.IsBreakEndNextDay
            })
            .ToDictionaryAsync(d => d.Id);

        return new LateEarlyLogCustomDataSet(_rosterService)
        {
            Employees = employees,
            EventsByEmployee = timeEvents.GroupBy(t => t.EmployeeId).ToDictionary(g => g.Key, g => g.ToList()),
            DutyPeriodsByEmployee = dutyPeriodAssignments
                .GroupBy(d => d.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList()),
            TimePolicies = timePolicies,
            Absences = absences,
            OrgAssignments = orgAssignments,
            SubstituteByEmployeeAndDate = substitutes
                .GroupBy(s => (s.EmployeeId, s.SubstituteDate.Date))
                .ToDictionary(g => g.Key, g => g.First().DutyId),
            Duties = duties,
            IsRosterActive = isRosterActive
        };
    }

    /// <summary>Legacy <c>TimeDataService.GetPublicHolidaysdatesList</c>, per employee.</summary>
    public async Task<HashSet<DateTime>> GetPublicHolidayDatesAsync(int employeeId, DateTime fromDate, DateTime toDate)
    {
        var holidays = await _timeDataService.GetPublicHolidaysDatesListAsync(employeeId, fromDate, toDate);
        return holidays.Select(h => h.HolidayDate.Date).ToHashSet();
    }
}

/// <summary>The two ways the four legacy controls' load regions differ. See the loader's remarks.</summary>
public class LateEarlyLogCustomLoadOptions
{
    /// <summary>
    /// Days to widen the punch window BACKWARD from <c>dtFrom</c>. Custom019 uses 1
    /// (uscRptLateEarlyLog_Custom019.ascx.cs:926); the base and the other three use 0.
    /// </summary>
    public int TimeEventLookbackDays { get; set; }

    /// <summary>
    /// True to load only <c>AbsenceHours == 0</c> absences, as Custom033/036 do in the query itself
    /// (uscRptLateEarlyLog_Custom033.ascx.cs:719). Those two have no partial-leave columns at all.
    /// </summary>
    public bool FullDayAbsencesOnly { get; set; }
}

/// <summary>
/// The materialized, window-bounded input sets plus the duty-resolution cascade all four variants share.
/// </summary>
public class LateEarlyLogCustomDataSet
{
    private readonly IRosterService _rosterService;

    public LateEarlyLogCustomDataSet(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    public IReadOnlyList<LateEarlyLogEmployeeInfo> Employees { get; init; }

    public Dictionary<int, List<LateEarlyLogTimeEventInfo>> EventsByEmployee { get; init; }

    public Dictionary<int, List<LateEarlyLogDutyPeriodInfo>> DutyPeriodsByEmployee { get; init; }

    public List<LateEarlyLogTimePolicyInfo> TimePolicies { get; init; }

    public List<LateEarlyLogAbsenceInfo> Absences { get; init; }

    public List<LateEarlyLogOrgAssignmentInfo> OrgAssignments { get; init; }

    public Dictionary<(int, DateTime), int> SubstituteByEmployeeAndDate { get; init; }

    public Dictionary<int, LateEarlyLogDutyInfo> Duties { get; init; }

    public bool IsRosterActive { get; init; }

    /// <summary>
    /// With the roster OFF, the employee's substitute duty for that exact date wins over the duty period's
    /// day-of-week Duty; with the roster ON, resolution is delegated entirely to the roster service.
    /// Identical in all four legacy controls (e.g. Custom033:635-654).
    /// </summary>
    public async Task<LateEarlyLogDutyInfo> ResolveDutyAsync(
        int employeeId,
        LateEarlyLogDutyPeriodInfo dutyPeriod,
        DateTime dayDate)
    {
        if (IsRosterActive)
        {
            // throwException: false -- one misconfigured duty period must not blank the whole report.
            var rosterDuty = await _rosterService.GetRosterDutyAsync(employeeId, dayDate, null, false);
            if (rosterDuty == null)
            {
                return null;
            }

            return Duties.TryGetValue(rosterDuty.Id, out var resolved) ? resolved : null;
        }

        if (SubstituteByEmployeeAndDate.TryGetValue((employeeId, dayDate.Date), out var substituteDutyId)
            && Duties.TryGetValue(substituteDutyId, out var substituteDuty))
        {
            return substituteDuty;
        }

        var dutyId = dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => dutyPeriod.SundayDutyId,
            DayOfWeek.Monday => dutyPeriod.MondayDutyId,
            DayOfWeek.Tuesday => dutyPeriod.TuesdayDutyId,
            DayOfWeek.Wednesday => dutyPeriod.WednesdayDutyId,
            DayOfWeek.Thursday => dutyPeriod.ThursdayDutyId,
            DayOfWeek.Friday => dutyPeriod.FridayDutyId,
            _ => dutyPeriod.SaturdayDutyId
        };

        return dutyId.HasValue && Duties.TryGetValue(dutyId.Value, out var weekdayDuty) ? weekdayDuty : null;
    }

    /// <summary>
    /// Legacy: the LATEST-starting duty period covering the day wins. Returns null when none covers it.
    /// </summary>
    public LateEarlyLogDutyPeriodInfo FindDutyPeriod(int employeeId, DateTime dayDate)
    {
        if (!DutyPeriodsByEmployee.TryGetValue(employeeId, out var employeeDutyPeriods))
        {
            return null;
        }

        return employeeDutyPeriods
            .Where(d => d.StartDate <= dayDate && d.EndDate >= dayDate)
            .OrderByDescending(d => d.StartDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// Legacy: punches from the day before, the day itself and the day after are all candidates, because
    /// a night shift straddles midnight in both directions.
    /// </summary>
    public List<LateEarlyLogTimeEventInfo> GetDayEvents(int employeeId, DateTime dayDate)
    {
        if (!EventsByEmployee.TryGetValue(employeeId, out var employeeEvents))
        {
            return new List<LateEarlyLogTimeEventInfo>();
        }

        return employeeEvents
            .Where(e => e.TransDate.Date == dayDate
                        || e.TransDate.Date == dayDate.AddDays(1)
                        || e.TransDate.Date == dayDate.AddDays(-1))
            .ToList();
    }

    public LateEarlyLogTimePolicyInfo FindTimePolicy(int employeeId, DateTime dayDate)
    {
        return TimePolicies.FirstOrDefault(p => p.EmployeeId == employeeId
                                                && p.StartDate <= dayDate
                                                && p.EndDate >= dayDate);
    }

    public LateEarlyLogOrgAssignmentInfo FindOrgAssignment(int employeeId, DateTime dayDate)
    {
        return OrgAssignments.FirstOrDefault(o => o.EmployeeId == employeeId
                                                  && o.StartDate <= dayDate
                                                  && o.EndDate >= dayDate);
    }

    /// <summary>
    /// Legacy's <c>dsCount &gt; 0</c> guard: an employee with NO duty-period assignment anywhere in the
    /// window contributes no rows at all, whatever their punches or absences say. Note that legacy checked
    /// only that SOME assignment exists, not that one covers the day -- the day check is separate.
    /// </summary>
    public bool HasAnyDutyPeriod(int employeeId)
    {
        return DutyPeriodsByEmployee.ContainsKey(employeeId);
    }

    /// <summary>
    /// The day name, with the hard-coded Arabic literal table legacy used rather than a resource lookup.
    /// Identical in all four controls. Reproduced verbatim.
    /// </summary>
    public static string GetWeekDayName(DateTime dayDate, bool useArabicNames)
    {
        if (!useArabicNames)
        {
            return dayDate.DayOfWeek.ToString();
        }

        return dayDate.DayOfWeek switch
        {
            DayOfWeek.Sunday => "الأحد",
            DayOfWeek.Monday => "الإثنين",
            DayOfWeek.Tuesday => "الثلاثاء",
            DayOfWeek.Wednesday => "الأربعاء",
            DayOfWeek.Thursday => "الخميس",
            DayOfWeek.Friday => "يوم الجمعة",
            _ => "يوم السبت"
        };
    }
}

public class LateEarlyLogEmployeeInfo
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string FullName { get; set; }

    public string AccessId { get; set; }
}

public class LateEarlyLogTimeEventInfo
{
    public int EmployeeId { get; set; }

    /// <summary>Legacy <c>TransType.Code</c> "1" is the clock-in event, "2" the clock-out event.</summary>
    public string TransTypeCode { get; set; }

    public DateTime TransDate { get; set; }

    public DateTime TransTime { get; set; }

    /// <summary>Legacy <c>MyTimeEvent.TransDateTime</c> -- the punch date plus the punch time-of-day.</summary>
    public DateTime TransDateTime { get; set; }

    public string WorkLocation { get; set; }

    public string AccessUnit { get; set; }
}

public class LateEarlyLogDutyPeriodInfo
{
    public int EmployeeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? SundayDutyId { get; set; }

    public int? MondayDutyId { get; set; }

    public int? TuesdayDutyId { get; set; }

    public int? WednesdayDutyId { get; set; }

    public int? ThursdayDutyId { get; set; }

    public int? FridayDutyId { get; set; }

    public int? SaturdayDutyId { get; set; }
}

public class LateEarlyLogDutyInfo
{
    public int Id { get; set; }

    public bool IsNightShift { get; set; }

    public bool IsWeeklyOff { get; set; }

    /// <summary>Used only by Custom019 and Custom023, which have a "Rest Day" status the base lacks.</summary>
    public bool IsRestDay { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public TimeSpan? FlexiStartTime { get; set; }

    public TimeSpan? FlexiEndTime { get; set; }

    public int? BreakShiftId { get; set; }

    public TimeSpan? BreakStartTime { get; set; }

    public TimeSpan? BreakEndTime { get; set; }

    public bool IsBreakStartNextDay { get; set; }

    public bool IsBreakEndNextDay { get; set; }
}

public class LateEarlyLogTimePolicyInfo
{
    public int EmployeeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string TimePolicyName { get; set; }

    public double ClockInLimit { get; set; }

    public double ClockOutLimit { get; set; }

    public double BreakInLimit { get; set; }

    public double BreakOutLimit { get; set; }

    public TimeEventRule TimeEventRule { get; set; }

    public TimeMgmtStatus TimeMgmtStatus { get; set; }

    public double OTMinLimit { get; set; }
}

public class LateEarlyLogAbsenceInfo
{
    public int EmployeeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string AbsenceTypeName { get; set; }

    public bool IsReturnDateRequired { get; set; }

    /// <summary>Used only by Custom019, whose full-day-leave row gates Absence Hours on it.</summary>
    public bool IsPaid { get; set; }

    public DateTime ReturnDate { get; set; }

    public double AbsenceHours { get; set; }
}

public class LateEarlyLogOrgAssignmentInfo
{
    public int EmployeeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool PayOvertime { get; set; }

    public string PayrollCountry { get; set; }

    public string WorkLocation { get; set; }

    public long? OrganizationUnitId { get; set; }
}
