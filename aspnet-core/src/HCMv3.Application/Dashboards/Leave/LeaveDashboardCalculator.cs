using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards.Dto;
using HCMv3.Dashboards.Leave.Dto;
using HCMv3.Workflow;

namespace HCMv3.Dashboards.Leave;

/// <summary>One leave row as the dashboard needs it: already scoped, already filtered by type.</summary>
public record LeaveDashboardLeaveFact(
    int AbsenceId,
    int EmployeeId,
    int AbsenceTypeId,
    DateTime StartDate,
    DateTime EndDate,
    double TotalLeaveDays,
    bool IsReturned,
    DateTime ReturnDate,
    bool IsReturnDateRequired,
    RequestStatus? WorkflowStatus);

/// <summary>An in-scope employee and the organisation unit they are assigned to today.</summary>
public record LeaveDashboardEmployee(
    int Id,
    string Code,
    string NameEn,
    string NameAr,
    bool IsActive,
    long? OrganizationUnitId);

public record LeaveDashboardAbsenceType(int Id, string NameEn, string NameAr);

/// <summary>
/// Every number on the Leave Dashboard, computed in memory from rows the app service has already scoped.
/// Kept free of repositories so the counting rules can be tested directly.
///
/// <para><b>Leave days</b> are the leave's own <c>TotalLeaveDays</c> (which already excludes weekends and
/// holidays per leave type), spread evenly over its calendar days. So a leave crossing a month edge or the
/// period edge counts only the share that falls inside. A row with no day count falls back to calendar days.</para>
///
/// <para><b>Department</b> is the unit the employee is assigned to today, not at the time of the leave. That
/// matches what HR means by "their department" and keeps drill-down consistent with the filter.</para>
/// </summary>
public class LeaveDashboardCalculator
{
    /// <summary>Longest period still shown day by day; anything longer is shown by month.</summary>
    public const int MaxDailyDays = 62;

    /// <summary>"Soon" means within this many days from today.</summary>
    public const int SoonDays = 7;

    private readonly DateTime _today;
    private readonly DateTime _from;
    private readonly DateTime _to;
    private readonly IReadOnlyDictionary<int, LeaveDashboardEmployee> _employees;
    private readonly DashboardOrgTree _tree;
    private readonly IReadOnlyDictionary<int, LeaveDashboardAbsenceType> _types;

    public LeaveDashboardCalculator(
        DateTime today,
        DateTime from,
        DateTime to,
        IEnumerable<LeaveDashboardEmployee> employees,
        IEnumerable<DashboardOrgUnit> orgUnits,
        IEnumerable<LeaveDashboardAbsenceType> absenceTypes)
    {
        _today = today.Date;
        _from = from.Date;
        _to = to.Date;
        _employees = employees.ToDictionary(e => e.Id);
        _tree = new DashboardOrgTree(orgUnits);
        _types = absenceTypes.ToDictionary(t => t.Id);
    }

    public LeaveDashboardGranularity Granularity =>
        (_to - _from).Days + 1 <= MaxDailyDays ? LeaveDashboardGranularity.Day : LeaveDashboardGranularity.Month;

    // ---------------------------------------------------------------- today

    public List<LeaveDashboardLeaveRowDto> OnLeaveToday(IEnumerable<LeaveDashboardLeaveFact> leaves)
    {
        return leaves
            .Where(l => IsActive(l.EmployeeId)
                        && l.StartDate.Date <= _today
                        && l.EndDate.Date >= _today
                        && !l.IsReturned)
            .OrderBy(l => l.EndDate)
            .ThenBy(l => EmployeeCode(l.EmployeeId))
            .Select(ToRow)
            .ToList();
    }

    /// <summary>
    /// The payroll engine's not-returned rule, the same one the Employee Workbench uses: a leave type that
    /// requires a return date, ended before today, with no return recorded.
    /// </summary>
    public List<LeaveDashboardLeaveRowDto> NotReturned(IEnumerable<LeaveDashboardLeaveFact> leaves)
    {
        return leaves
            .Where(l => IsActive(l.EmployeeId)
                        && l.IsReturnDateRequired
                        && !l.IsReturned
                        && l.ReturnDate == default
                        && l.EndDate.Date < _today)
            .Select(ToRow)
            .OrderByDescending(r => r.DaysOverdue)
            .ThenBy(r => r.EmployeeCode)
            .ToList();
    }

    public List<LeaveDashboardLeaveRowDto> StartingSoon(IEnumerable<LeaveDashboardLeaveFact> leaves)
    {
        var until = _today.AddDays(SoonDays);

        return leaves
            .Where(l => IsActive(l.EmployeeId) && l.StartDate.Date > _today && l.StartDate.Date <= until)
            .OrderBy(l => l.StartDate)
            .ThenBy(l => EmployeeCode(l.EmployeeId))
            .Select(ToRow)
            .ToList();
    }

    /// <summary>
    /// Due back within a week: the leave ends today or in the next six days, so the first day back is within
    /// seven days. Only leave types that expect a return, the same ones the not-returned list watches -- a
    /// one-day sick leave "returning tomorrow" is noise.
    /// </summary>
    public List<LeaveDashboardLeaveRowDto> ReturningSoon(IEnumerable<LeaveDashboardLeaveFact> leaves)
    {
        var until = _today.AddDays(SoonDays);

        return leaves
            .Where(l => IsActive(l.EmployeeId)
                        && l.IsReturnDateRequired
                        && !l.IsReturned
                        && l.StartDate.Date <= _today.AddDays(SoonDays)
                        && l.EndDate.Date >= _today
                        && l.EndDate.Date < until)
            .OrderBy(l => l.EndDate)
            .ThenBy(l => EmployeeCode(l.EmployeeId))
            .Select(ToRow)
            .ToList();
    }

    public List<LeaveDashboardLeaveRowDto> PendingApproval(IEnumerable<LeaveDashboardLeaveFact> leaves)
    {
        return leaves
            .Where(l => IsActive(l.EmployeeId) && l.WorkflowStatus == RequestStatus.Open)
            .OrderBy(l => l.StartDate)
            .ThenBy(l => EmployeeCode(l.EmployeeId))
            .Select(ToRow)
            .ToList();
    }

    public static int DistinctEmployees(IEnumerable<LeaveDashboardLeaveRowDto> rows) =>
        rows.Select(r => r.EmployeeId).Distinct().Count();

    // ---------------------------------------------------------------- period

    public bool OverlapsPeriod(LeaveDashboardLeaveFact leave) =>
        leave.StartDate.Date <= _to && leave.EndDate.Date >= _from;

    public bool Covers(LeaveDashboardLeaveFact leave, DateTime day) =>
        leave.StartDate.Date <= day.Date && leave.EndDate.Date >= day.Date;

    public double LeaveDaysInPeriod(IEnumerable<LeaveDashboardLeaveFact> leaves) =>
        Round(leaves.Sum(l => LeaveDaysWithin(l, _from, _to)));

    /// <summary>
    /// The share of <paramref name="leave"/> that falls inside [<paramref name="start"/>, <paramref name="end"/>],
    /// in leave days. See the class remarks for how days are spread.
    /// </summary>
    public static double LeaveDaysWithin(LeaveDashboardLeaveFact leave, DateTime start, DateTime end)
    {
        var leaveStart = leave.StartDate.Date;
        var leaveEnd = leave.EndDate.Date;
        if (leaveEnd < leaveStart)
        {
            return 0;
        }

        var overlapStart = leaveStart > start.Date ? leaveStart : start.Date;
        var overlapEnd = leaveEnd < end.Date ? leaveEnd : end.Date;
        if (overlapEnd < overlapStart)
        {
            return 0;
        }

        double overlapDays = (overlapEnd - overlapStart).Days + 1;
        if (leave.TotalLeaveDays <= 0)
        {
            return overlapDays;
        }

        double calendarDays = (leaveEnd - leaveStart).Days + 1;
        return leave.TotalLeaveDays * overlapDays / calendarDays;
    }

    /// <summary>
    /// Monthly buckets carry leave days per type; daily buckets carry how many employees of each type were
    /// out that day. Every bucket in the period is returned, empty or not, so the axis has no gaps.
    /// </summary>
    public List<LeaveDashboardTrendPointDto> Trend(IReadOnlyCollection<LeaveDashboardLeaveFact> leaves)
    {
        var points = new List<LeaveDashboardTrendPointDto>();

        if (Granularity == LeaveDashboardGranularity.Day)
        {
            for (var day = _from; day <= _to; day = day.AddDays(1))
            {
                var current = day;
                points.Add(new LeaveDashboardTrendPointDto
                {
                    PeriodStart = current,
                    Values = leaves
                        .Where(l => Covers(l, current))
                        .GroupBy(l => l.AbsenceTypeId)
                        .Select(g => new LeaveDashboardTrendValueDto
                        {
                            AbsenceTypeId = g.Key,
                            Value = g.Select(l => l.EmployeeId).Distinct().Count()
                        })
                        .OrderBy(v => v.AbsenceTypeId)
                        .ToList()
                });
            }

            return points;
        }

        for (var month = new DateTime(_from.Year, _from.Month, 1); month <= _to; month = month.AddMonths(1))
        {
            var windowStart = month < _from ? _from : month;
            var monthEnd = month.AddMonths(1).AddDays(-1);
            var windowEnd = monthEnd > _to ? _to : monthEnd;

            points.Add(new LeaveDashboardTrendPointDto
            {
                PeriodStart = month,
                Values = leaves
                    .Select(l => new { l.AbsenceTypeId, Days = LeaveDaysWithin(l, windowStart, windowEnd) })
                    .Where(x => x.Days > 0)
                    .GroupBy(x => x.AbsenceTypeId)
                    .Select(g => new LeaveDashboardTrendValueDto { AbsenceTypeId = g.Key, Value = Round(g.Sum(x => x.Days)) })
                    .OrderBy(v => v.AbsenceTypeId)
                    .ToList()
            });
        }

        return points;
    }

    public List<LeaveDashboardTypeBucketDto> ByType(IEnumerable<LeaveDashboardLeaveFact> leaves)
    {
        return leaves
            .Where(OverlapsPeriod)
            .GroupBy(l => l.AbsenceTypeId)
            .Select(g =>
            {
                _types.TryGetValue(g.Key, out var type);
                return new LeaveDashboardTypeBucketDto
                {
                    AbsenceTypeId = g.Key,
                    NameEn = type?.NameEn,
                    NameAr = type?.NameAr,
                    Employees = g.Select(l => l.EmployeeId).Distinct().Count(),
                    LeaveDays = Round(g.Sum(l => LeaveDaysWithin(l, _from, _to)))
                };
            })
            .Where(b => b.LeaveDays > 0)
            .OrderByDescending(b => b.LeaveDays)
            .ToList();
    }

    /// <summary>
    /// Leave per unit one level below <paramref name="selectedUnitId"/> (or per top-level unit when nothing is
    /// selected). Employees sitting directly in the selected unit get a bucket of their own, flagged
    /// <see cref="LeaveDashboardOrgUnitBucketDto.IsDirectStaff"/>, so clicking it does not loop.
    /// </summary>
    public List<LeaveDashboardOrgUnitBucketDto> ByOrgUnit(IEnumerable<LeaveDashboardLeaveFact> leaves, long? selectedUnitId)
    {
        var buckets = new Dictionary<(long? Id, bool Direct), (List<int> Employees, double Days)>();

        foreach (var leave in leaves.Where(OverlapsPeriod))
        {
            if (!_employees.TryGetValue(leave.EmployeeId, out var employee))
            {
                continue;
            }

            var bucket = _tree.BucketFor(employee.OrganizationUnitId, selectedUnitId);
            if (bucket == null)
            {
                continue;
            }

            if (!buckets.TryGetValue(bucket.Value, out var totals))
            {
                totals = (new List<int>(), 0);
            }

            totals.Employees.Add(leave.EmployeeId);
            totals.Days += LeaveDaysWithin(leave, _from, _to);
            buckets[bucket.Value] = totals;
        }

        return buckets
            .Select(b => new LeaveDashboardOrgUnitBucketDto
            {
                OrganizationUnitId = b.Key.Id,
                IsDirectStaff = b.Key.Direct,
                Name = b.Key.Id.HasValue ? _tree.NameOf(b.Key.Id.Value) : null,
                HasChildren = !b.Key.Direct && b.Key.Id.HasValue && _tree.HasChildren(b.Key.Id.Value),
                Employees = b.Value.Employees.Distinct().Count(),
                LeaveDays = Round(b.Value.Days)
            })
            .Where(b => b.LeaveDays > 0)
            .OrderByDescending(b => b.LeaveDays)
            .ThenBy(b => b.Name)
            .ToList();
    }

    // ---------------------------------------------------------------- organisation tree (shared: DashboardOrgTree)

    /// <summary>The unit itself and every unit below it.</summary>
    public HashSet<long> SelfAndDescendants(long unitId) => _tree.SelfAndDescendants(unitId);

    /// <summary>Top-down breadcrumb to <paramref name="unitId"/>.</summary>
    public List<DashboardOrgUnitOptionDto> PathTo(long? unitId) => _tree.PathTo(unitId);

    /// <summary>
    /// The units a filter dropdown should offer: those holding at least one of <paramref name="employees"/>,
    /// plus their ancestors so the tree reads properly. A manager is not shown the rest of the company.
    /// </summary>
    public List<DashboardOrgUnitOptionDto> OrgUnitOptions(IEnumerable<LeaveDashboardEmployee> employees) =>
        _tree.OptionsFor(employees.Select(e => e.OrganizationUnitId));

    // ---------------------------------------------------------------- rows

    public LeaveDashboardLeaveRowDto ToRow(LeaveDashboardLeaveFact leave)
    {
        _employees.TryGetValue(leave.EmployeeId, out var employee);
        _types.TryGetValue(leave.AbsenceTypeId, out var type);

        var unitName = employee?.OrganizationUnitId is long unitId ? _tree.NameOf(unitId) : null;

        return new LeaveDashboardLeaveRowDto
        {
            AbsenceId = leave.AbsenceId,
            EmployeeId = leave.EmployeeId,
            EmployeeCode = employee?.Code,
            EmployeeNameEn = employee?.NameEn,
            EmployeeNameAr = employee?.NameAr,
            OrganizationUnitName = unitName,
            AbsenceTypeId = leave.AbsenceTypeId,
            AbsenceTypeNameEn = type?.NameEn,
            AbsenceTypeNameAr = type?.NameAr,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            TotalLeaveDays = leave.TotalLeaveDays,
            DaysOverdue = leave.EndDate.Date < _today ? (_today - leave.EndDate.Date).Days : 0,
            WorkflowStatus = leave.WorkflowStatus
        };
    }

    private bool IsActive(int employeeId) =>
        _employees.TryGetValue(employeeId, out var employee) && employee.IsActive;

    private string EmployeeCode(int employeeId) =>
        _employees.TryGetValue(employeeId, out var employee) ? employee.Code : null;

    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
}
