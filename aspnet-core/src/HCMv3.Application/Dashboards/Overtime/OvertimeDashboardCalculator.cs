using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards.Overtime.Dto;
using HCMv3.Workflow;

namespace HCMv3.Dashboards.Overtime;

/// <summary>One overtime entry: claimed, actual and approved hours of one type for one employee in one month.</summary>
public record OvertimeEntry(
    int EmployeeId,
    DateTime EffectiveDate,
    int OvertimeTypeId,
    double ClaimedHours,
    double ActualHours,
    double ApprovedHours,
    RequestStatus? Status);

/// <summary>What payroll paid one employee under one overtime type's wage type for one month.</summary>
public record OvertimeCost(int EmployeeId, DateTime Month, int OvertimeTypeId, double Amount);

public record OvertimeEmployee(int Id, string Code, string NameEn, string NameAr, long? OrganizationUnitId);

/// <summary>
/// Every number on the Overtime Dashboard, computed in memory from entries and payroll amounts the app service has
/// already scoped. Kept free of repositories so the rules can be tested directly.
///
/// <para><b>Hours</b> are always the <b>approved</b> hours of approved entries -- what payroll pays on. <b>Cost</b> is
/// not estimated from a rate: it is what payroll actually paid under each overtime type's wage type, from the
/// regular result of each month (the one calculated in the month it is for), so a month not yet run by payroll has
/// no cost yet. Legacy multiplied the hours by today's basic pay, which was wrong for any past month and crashed for
/// anyone without a basic-pay row.</para>
/// </summary>
public class OvertimeDashboardCalculator
{
    public const int TopCount = 10;

    private readonly DateTime _from;
    private readonly DateTime _to;
    private readonly DashboardOrgTree _tree;
    private readonly IReadOnlyDictionary<int, OvertimeEmployee> _employees;

    public OvertimeDashboardCalculator(DateTime from, DateTime to, DashboardOrgTree tree, IEnumerable<OvertimeEmployee> employees)
    {
        _from = from.Date;
        _to = to.Date;
        _tree = tree;
        _employees = employees.ToDictionary(e => e.Id);
    }

    public static DateTime MonthOf(DateTime date) => new(date.Year, date.Month, 1);

    public OvertimeKpisDto Kpis(IReadOnlyCollection<OvertimeEntry> entries, IReadOnlyCollection<OvertimeCost> costs, IReadOnlyCollection<OvertimeEntry> pending)
    {
        var approved = entries.Sum(e => e.ApprovedHours);
        var claimed = entries.Sum(e => e.ClaimedHours);
        var employees = entries.Where(e => e.ApprovedHours > 0).Select(e => e.EmployeeId).Distinct().Count();

        return new OvertimeKpisDto
        {
            ApprovedHours = Round(approved),
            ClaimedHours = Round(claimed),
            ApprovedOfClaimedPercent = claimed <= 0 ? null : Round(approved * 100 / claimed),
            Employees = employees,
            AverageHoursPerEmployee = employees == 0 ? null : Round(approved / employees),
            Cost = Money(costs.Sum(c => c.Amount)),
            PendingEntries = pending.Count,
            PendingHours = Round(pending.Sum(e => e.ClaimedHours))
        };
    }

    /// <summary>One point per month of the period, hours split by type, with that month's paid cost.</summary>
    public List<OvertimeTrendPointDto> Trend(IReadOnlyCollection<OvertimeEntry> entries, IReadOnlyCollection<OvertimeCost> costs)
    {
        var hoursByMonth = entries.ToLookup(e => MonthOf(e.EffectiveDate));
        var costByMonth = costs.GroupBy(c => MonthOf(c.Month)).ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));

        var points = new List<OvertimeTrendPointDto>();
        for (var month = MonthOf(_from); month <= _to; month = month.AddMonths(1))
        {
            points.Add(new OvertimeTrendPointDto
            {
                MonthStart = month,
                Hours = hoursByMonth[month]
                    .GroupBy(e => e.OvertimeTypeId)
                    .Select(g => new OvertimeTrendValueDto { OvertimeTypeId = g.Key, Hours = Round(g.Sum(e => e.ApprovedHours)) })
                    .Where(v => v.Hours > 0)
                    .OrderBy(v => v.OvertimeTypeId)
                    .ToList(),
                Cost = Money(costByMonth.GetValueOrDefault(month))
            });
        }

        return points;
    }

    public static List<OvertimeTypeBucketDto> ByType(IReadOnlyCollection<OvertimeEntry> entries, IReadOnlyCollection<OvertimeCost> costs)
    {
        var costByType = costs.GroupBy(c => c.OvertimeTypeId).ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));

        return entries.GroupBy(e => e.OvertimeTypeId)
            .Select(g => new OvertimeTypeBucketDto
            {
                OvertimeTypeId = g.Key,
                Hours = Round(g.Sum(e => e.ApprovedHours)),
                Employees = g.Where(e => e.ApprovedHours > 0).Select(e => e.EmployeeId).Distinct().Count(),
                Cost = Money(costByType.GetValueOrDefault(g.Key))
            })
            .Where(b => b.Hours > 0)
            .OrderByDescending(b => b.Hours)
            .ToList();
    }

    public List<OvertimeOrgUnitBucketDto> ByOrgUnit(IReadOnlyCollection<OvertimeEntry> entries, IReadOnlyCollection<OvertimeCost> costs, long? selectedUnitId)
    {
        var buckets = new Dictionary<(long? Id, bool Direct), (List<OvertimeEntry> Entries, double Cost)>();

        (long? Id, bool Direct)? BucketOf(int employeeId) =>
            _employees.TryGetValue(employeeId, out var e) ? _tree.BucketFor(e.OrganizationUnitId, selectedUnitId) : null;

        foreach (var entry in entries)
        {
            var bucket = BucketOf(entry.EmployeeId);
            if (bucket == null)
            {
                continue;
            }

            var current = buckets.GetValueOrDefault(bucket.Value, (new List<OvertimeEntry>(), 0));
            current.Entries.Add(entry);
            buckets[bucket.Value] = current;
        }

        foreach (var cost in costs)
        {
            var bucket = BucketOf(cost.EmployeeId);
            if (bucket == null)
            {
                continue;
            }

            var current = buckets.GetValueOrDefault(bucket.Value, (new List<OvertimeEntry>(), 0));
            buckets[bucket.Value] = (current.Entries, current.Cost + cost.Amount);
        }

        return buckets
            .Select(b => new OvertimeOrgUnitBucketDto
            {
                OrganizationUnitId = b.Key.Id,
                IsDirectStaff = b.Key.Direct,
                Name = b.Key.Id.HasValue ? _tree.NameOf(b.Key.Id.Value) : null,
                HasChildren = !b.Key.Direct && b.Key.Id.HasValue && _tree.HasChildren(b.Key.Id.Value),
                Hours = Round(b.Value.Entries.Sum(e => e.ApprovedHours)),
                Employees = b.Value.Entries.Where(e => e.ApprovedHours > 0).Select(e => e.EmployeeId).Distinct().Count(),
                Cost = Money(b.Value.Cost)
            })
            .Where(b => b.Hours > 0 || b.Cost > 0)
            .OrderByDescending(b => b.Hours)
            .ThenBy(b => b.Name)
            .ToList();
    }

    public List<OvertimeTopEmployeeDto> TopEmployees(IReadOnlyCollection<OvertimeEntry> entries, IReadOnlyCollection<OvertimeCost> costs)
    {
        var costByEmployee = costs.GroupBy(c => c.EmployeeId).ToDictionary(g => g.Key, g => g.Sum(c => c.Amount));

        return entries.GroupBy(e => e.EmployeeId)
            .Select(g =>
            {
                _employees.TryGetValue(g.Key, out var e);
                return new OvertimeTopEmployeeDto
                {
                    EmployeeId = g.Key,
                    EmployeeCode = e?.Code,
                    EmployeeNameEn = e?.NameEn,
                    EmployeeNameAr = e?.NameAr,
                    Hours = Round(g.Sum(x => x.ApprovedHours)),
                    Cost = Money(costByEmployee.GetValueOrDefault(g.Key))
                };
            })
            .Where(t => t.Hours > 0)
            .OrderByDescending(t => t.Hours)
            .ThenBy(t => t.EmployeeCode)
            .Take(TopCount)
            .ToList();
    }

    public OvertimeEntryRowDto ToRow(OvertimeEntry entry)
    {
        _employees.TryGetValue(entry.EmployeeId, out var e);
        return new OvertimeEntryRowDto
        {
            EmployeeId = entry.EmployeeId,
            EmployeeCode = e?.Code,
            EmployeeNameEn = e?.NameEn,
            EmployeeNameAr = e?.NameAr,
            OrganizationUnitName = e?.OrganizationUnitId is long unitId ? _tree.NameOf(unitId) : null,
            EffectiveDate = entry.EffectiveDate,
            OvertimeTypeId = entry.OvertimeTypeId,
            ClaimedHours = entry.ClaimedHours,
            ActualHours = entry.ActualHours,
            ApprovedHours = entry.ApprovedHours,
            Status = entry.Status
        };
    }

    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    /// <summary>Kuwaiti dinar has three decimals (fils).</summary>
    private static double Money(double value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
