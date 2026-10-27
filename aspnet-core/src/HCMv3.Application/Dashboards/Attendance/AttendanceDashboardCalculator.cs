using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards.Attendance.Dto;

namespace HCMv3.Dashboards.Attendance;

/// <summary>
/// One evaluated attendance day, as the Time Engine left it in <c>TimeRegister</c>. <see cref="Absent"/> is already
/// resolved against the tenant's approval setting (approved absence when time-register approval is on).
/// A punch the day did not get is null.
/// </summary>
public record AttendanceDay(
    int EmployeeId,
    DateTime DayDate,
    bool Planned,
    bool OnLeave,
    bool Off,
    bool Absent,
    double LateMinutes,
    double EarlyMinutes,
    DateTime? PlannedIn,
    DateTime? PlannedOut,
    DateTime? ActualIn,
    DateTime? ActualOut,
    double PlannedMinutes,
    double ActualMinutes);

public record AttendanceEmployee(int Id, string Code, string NameEn, string NameAr, long? OrganizationUnitId);

/// <summary>
/// Every number on the Time &amp; Attendance Dashboard, computed in memory from days the app service has already
/// scoped. Kept free of repositories so the rules can be tested directly.
///
/// <para><b>A working day</b> is one the employee was scheduled to work: a planned working day that is not leave, a
/// public holiday, a weekly off or a rest day -- the same test the Employee Workbench uses. Every rate is over
/// working days. <b>Present</b> is a working day that is not absent; lateness, early departure and missing
/// punches are only looked for on present days.</para>
///
/// <para><b>A missing punch</b> is a present day with an out-punch but no in-punch, or an in-punch but no out-punch
/// (not counted for today, when the employee may simply not have left yet).</para>
/// </summary>
public class AttendanceDashboardCalculator
{
    public const int MaxDailyDays = 62;
    public const int TopCount = 10;

    private readonly DateTime _from;
    private readonly DateTime _to;
    private readonly DateTime _today;
    private readonly DashboardOrgTree _tree;
    private readonly IReadOnlyDictionary<int, AttendanceEmployee> _employees;

    public AttendanceDashboardCalculator(
        DateTime from,
        DateTime to,
        DateTime today,
        DashboardOrgTree tree,
        IEnumerable<AttendanceEmployee> employees)
    {
        _from = from.Date;
        _to = to.Date;
        _today = today.Date;
        _tree = tree;
        _employees = employees.ToDictionary(e => e.Id);
    }

    public AttendanceDashboardGranularity Granularity =>
        (_to - _from).Days + 1 <= MaxDailyDays ? AttendanceDashboardGranularity.Day : AttendanceDashboardGranularity.Month;

    // ---------------------------------------------------------------- rules

    public static bool IsWorkingDay(AttendanceDay d) => d.Planned && !d.OnLeave && !d.Off;

    public static bool IsAbsent(AttendanceDay d) => IsWorkingDay(d) && d.Absent;

    public static bool IsPresent(AttendanceDay d) => IsWorkingDay(d) && !d.Absent;

    public static bool IsLate(AttendanceDay d) => IsPresent(d) && d.LateMinutes > 0;

    public static bool IsEarly(AttendanceDay d) => IsPresent(d) && d.EarlyMinutes > 0;

    public static bool IsMissingIn(AttendanceDay d) => IsPresent(d) && d.ActualIn == null && d.ActualOut != null;

    public bool IsMissingOut(AttendanceDay d) =>
        IsPresent(d) && d.ActualIn != null && d.ActualOut == null && d.DayDate.Date < _today;

    public bool IsMissingPunch(AttendanceDay d) => IsMissingIn(d) || IsMissingOut(d);

    public bool Matches(AttendanceDay d, AttendanceExceptionKind kind) => kind switch
    {
        AttendanceExceptionKind.Absent => IsAbsent(d),
        AttendanceExceptionKind.Late => IsLate(d),
        AttendanceExceptionKind.EarlyOut => IsEarly(d),
        AttendanceExceptionKind.MissingPunch => IsMissingPunch(d),
        _ => IsAbsent(d) || IsLate(d) || IsEarly(d) || IsMissingPunch(d)
    };

    public static double? Rate(int present, int working) =>
        working == 0 ? null : Round(present * 100.0 / working);

    // ---------------------------------------------------------------- figures

    public AttendanceKpisDto Kpis(IReadOnlyCollection<AttendanceDay> days)
    {
        var working = days.Where(IsWorkingDay).ToList();
        var present = working.Count(d => !d.Absent);

        return new AttendanceKpisDto
        {
            WorkingDays = working.Count,
            PresentDays = present,
            AbsentDays = working.Count - present,
            AttendanceRatePercent = Rate(present, working.Count),
            LateDays = days.Count(IsLate),
            LateHours = Round(days.Where(IsLate).Sum(d => d.LateMinutes) / 60),
            EarlyDays = days.Count(IsEarly),
            EarlyHours = Round(days.Where(IsEarly).Sum(d => d.EarlyMinutes) / 60),
            MissingPunchDays = days.Count(IsMissingPunch),
            PlannedHours = Round(working.Sum(d => d.PlannedMinutes) / 60),
            ActualHours = Round(working.Sum(d => d.ActualMinutes) / 60)
        };
    }

    /// <summary>Every bucket of the period, empty ones included so the axis has no gaps.</summary>
    public List<AttendanceTrendPointDto> Trend(IReadOnlyCollection<AttendanceDay> days)
    {
        var daily = Granularity == AttendanceDashboardGranularity.Day;
        var byBucket = days.ToLookup(d => daily ? d.DayDate.Date : new DateTime(d.DayDate.Year, d.DayDate.Month, 1));

        var points = new List<AttendanceTrendPointDto>();
        var start = daily ? _from : new DateTime(_from.Year, _from.Month, 1);
        for (var bucket = start; bucket <= _to; bucket = daily ? bucket.AddDays(1) : bucket.AddMonths(1))
        {
            var inBucket = byBucket[bucket].ToList();
            var working = inBucket.Count(IsWorkingDay);
            var present = inBucket.Count(IsPresent);

            points.Add(new AttendanceTrendPointDto
            {
                PeriodStart = bucket,
                WorkingDays = working,
                AbsentDays = working - present,
                LateDays = inBucket.Count(IsLate),
                EarlyDays = inBucket.Count(IsEarly),
                AttendanceRatePercent = Rate(present, working)
            });
        }

        return points;
    }

    public List<AttendanceOrgUnitBucketDto> ByOrgUnit(IReadOnlyCollection<AttendanceDay> days, long? selectedUnitId)
    {
        var buckets = new Dictionary<(long? Id, bool Direct), List<AttendanceDay>>();

        foreach (var day in days)
        {
            if (!_employees.TryGetValue(day.EmployeeId, out var employee))
            {
                continue;
            }

            var bucket = _tree.BucketFor(employee.OrganizationUnitId, selectedUnitId);
            if (bucket == null)
            {
                continue;
            }

            if (!buckets.TryGetValue(bucket.Value, out var list))
            {
                buckets[bucket.Value] = list = new List<AttendanceDay>();
            }

            list.Add(day);
        }

        return buckets
            .Select(b =>
            {
                var working = b.Value.Count(IsWorkingDay);
                var present = b.Value.Count(IsPresent);
                return new AttendanceOrgUnitBucketDto
                {
                    OrganizationUnitId = b.Key.Id,
                    IsDirectStaff = b.Key.Direct,
                    Name = b.Key.Id.HasValue ? _tree.NameOf(b.Key.Id.Value) : null,
                    HasChildren = !b.Key.Direct && b.Key.Id.HasValue && _tree.HasChildren(b.Key.Id.Value),
                    WorkingDays = working,
                    AbsentDays = working - present,
                    LateDays = b.Value.Count(IsLate),
                    EarlyDays = b.Value.Count(IsEarly),
                    AttendanceRatePercent = Rate(present, working)
                };
            })
            .Where(b => b.WorkingDays > 0)
            .OrderBy(b => b.AttendanceRatePercent)
            .ThenBy(b => b.Name)
            .ToList();
    }

    public List<AttendanceTopEmployeeDto> TopAbsent(IReadOnlyCollection<AttendanceDay> days) =>
        Top(days.Where(IsAbsent), g => g.Count());

    /// <summary>In hours.</summary>
    public List<AttendanceTopEmployeeDto> TopLate(IReadOnlyCollection<AttendanceDay> days) =>
        Top(days.Where(IsLate), g => Round(g.Sum(d => d.LateMinutes) / 60));

    /// <summary>In hours.</summary>
    public List<AttendanceTopEmployeeDto> TopEarly(IReadOnlyCollection<AttendanceDay> days) =>
        Top(days.Where(IsEarly), g => Round(g.Sum(d => d.EarlyMinutes) / 60));

    private List<AttendanceTopEmployeeDto> Top(IEnumerable<AttendanceDay> days, Func<IGrouping<int, AttendanceDay>, double> value) =>
        days.GroupBy(d => d.EmployeeId)
            .Select(g =>
            {
                _employees.TryGetValue(g.Key, out var e);
                return new AttendanceTopEmployeeDto
                {
                    EmployeeId = g.Key,
                    EmployeeCode = e?.Code,
                    EmployeeNameEn = e?.NameEn,
                    EmployeeNameAr = e?.NameAr,
                    Value = value(g),
                    Days = g.Count()
                };
            })
            .OrderByDescending(t => t.Value)
            .ThenBy(t => t.EmployeeCode)
            .Take(TopCount)
            .ToList();

    /// <summary>Late arrivals and absences per day of the week, Sunday first.</summary>
    public static List<AttendanceWeekdayDto> Weekdays(IReadOnlyCollection<AttendanceDay> days) =>
        Enumerable.Range(0, 7)
            .Select(dow => new AttendanceWeekdayDto
            {
                DayOfWeek = dow,
                LateDays = days.Count(d => (int)d.DayDate.DayOfWeek == dow && IsLate(d)),
                AbsentDays = days.Count(d => (int)d.DayDate.DayOfWeek == dow && IsAbsent(d))
            })
            .ToList();

    public AttendanceExceptionRowDto ToRow(AttendanceDay d)
    {
        _employees.TryGetValue(d.EmployeeId, out var e);
        return new AttendanceExceptionRowDto
        {
            EmployeeId = d.EmployeeId,
            EmployeeCode = e?.Code,
            EmployeeNameEn = e?.NameEn,
            EmployeeNameAr = e?.NameAr,
            OrganizationUnitName = e?.OrganizationUnitId is long unitId ? _tree.NameOf(unitId) : null,
            DayDate = d.DayDate,
            PlannedIn = d.PlannedIn,
            PlannedOut = d.PlannedOut,
            ActualIn = d.ActualIn,
            ActualOut = d.ActualOut,
            IsAbsent = IsAbsent(d),
            LateMinutes = IsLate(d) ? d.LateMinutes : 0,
            EarlyMinutes = IsEarly(d) ? d.EarlyMinutes : 0,
            IsMissingIn = IsMissingIn(d),
            IsMissingOut = IsMissingOut(d)
        };
    }

    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
}
