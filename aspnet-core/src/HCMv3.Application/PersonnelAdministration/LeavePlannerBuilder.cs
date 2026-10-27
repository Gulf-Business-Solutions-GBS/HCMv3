using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>An employee as the planner needs them: identity and the org unit they sit in today.</summary>
public sealed record LeavePlannerEmployee(int Id, string Code, string FullNameEn, string FullNameAr, long? OrganizationUnitId);

/// <summary>One plan or one leave, already classified as approved or pending.</summary>
public sealed record LeavePlannerFact(
    int EmployeeId,
    LeavePlannerEntryKind Kind,
    int Id,
    DateTime StartDate,
    DateTime EndDate,
    string LeaveTypeNameEn = null,
    string LeaveTypeNameAr = null);

/// <summary>
/// Lays plans and leave out as an employee-by-day grid for one month, grouped by organization unit -- the
/// legacy <c>ASPxScheduler</c> of <c>uscLeavePlanner</c> rendered as a plain matrix, the same way the roster
/// schedule viewer renders legacy's other scheduler.
///
/// <para>
/// Pure, with no repositories, so the layout rules are tested directly: only employees with something in the
/// month get a row (legacy showed appointments, not people); entries are clipped to the month; a day carries
/// every kind that falls on it, because a plan sitting on top of approved leave is exactly what a manager looks
/// at the planner to find.
/// </para>
/// </summary>
public static class LeavePlannerBuilder
{
    public static LeavePlannerDto Build(
        DateTime fromDate,
        DateTime toDate,
        IEnumerable<LeavePlannerEmployee> employees,
        IEnumerable<LeavePlannerFact> facts,
        DashboardOrgTree tree,
        string unassignedName)
    {
        var from = fromDate.Date;
        var to = toDate.Date;
        var dayCount = Math.Max(0, (to - from).Days + 1);

        var employeesById = employees.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First());

        var rows = new List<(LeavePlannerEmployee Employee, LeavePlannerRowDto Row)>();

        foreach (var byEmployee in facts
                     .Where(f => f.StartDate.Date <= to && f.EndDate.Date >= from && f.EndDate.Date >= f.StartDate.Date)
                     .GroupBy(f => f.EmployeeId))
        {
            if (!employeesById.TryGetValue(byEmployee.Key, out var employee))
            {
                continue;
            }

            var row = new LeavePlannerRowDto
            {
                EmployeeId = employee.Id,
                EmployeeCode = employee.Code,
                EmployeeFullNameEn = employee.FullNameEn,
                EmployeeFullNameAr = employee.FullNameAr,
                Days = Enumerable.Repeat(LeavePlannerDayFlags.None, dayCount).ToList()
            };

            foreach (var fact in byEmployee.OrderBy(f => f.StartDate).ThenBy(f => f.Kind))
            {
                var start = fact.StartDate.Date < from ? from : fact.StartDate.Date;
                var end = fact.EndDate.Date > to ? to : fact.EndDate.Date;
                var flag = FlagOf(fact.Kind);

                for (var day = start; day <= end; day = day.AddDays(1))
                {
                    row.Days[(day - from).Days] |= flag;
                }

                row.Entries.Add(new LeavePlannerEntryDto
                {
                    Kind = fact.Kind,
                    Id = fact.Id,
                    StartDate = fact.StartDate.Date,
                    EndDate = fact.EndDate.Date,
                    LeaveTypeNameEn = fact.LeaveTypeNameEn,
                    LeaveTypeNameAr = fact.LeaveTypeNameAr
                });
            }

            rows.Add((employee, row));
        }

        var groups = rows
            .GroupBy(r => r.Employee.OrganizationUnitId.HasValue && tree.Contains(r.Employee.OrganizationUnitId.Value)
                ? r.Employee.OrganizationUnitId
                : null)
            .Select(g => new LeavePlannerGroupDto
            {
                OrganizationUnitId = g.Key,
                OrganizationUnitName = g.Key.HasValue ? tree.NameOf(g.Key.Value) : unassignedName,
                Rows = g.Select(r => r.Row).OrderBy(r => r.EmployeeCode, StringComparer.OrdinalIgnoreCase).ToList(),
                AwayCountByDay = Enumerable.Range(0, dayCount)
                    .Select(i => g.Count(r => r.Row.Days[i] != LeavePlannerDayFlags.None))
                    .ToList()
            })
            // Units by name, and the employees nobody has placed in a unit last rather than first.
            .OrderBy(g => g.OrganizationUnitId.HasValue ? 0 : 1)
            .ThenBy(g => g.OrganizationUnitName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new LeavePlannerDto
        {
            FromDate = from,
            ToDate = to,
            Groups = groups
        };
    }

    /// <summary>The calendar month containing <paramref name="anyDay"/>.</summary>
    public static (DateTime From, DateTime To) MonthOf(DateTime anyDay)
    {
        var from = new DateTime(anyDay.Year, anyDay.Month, 1);
        return (from, from.AddMonths(1).AddDays(-1));
    }

    private static LeavePlannerDayFlags FlagOf(LeavePlannerEntryKind kind)
    {
        switch (kind)
        {
            case LeavePlannerEntryKind.PlannedApproved:
                return LeavePlannerDayFlags.PlannedApproved;
            case LeavePlannerEntryKind.PlannedPending:
                return LeavePlannerDayFlags.PlannedPending;
            case LeavePlannerEntryKind.LeaveApproved:
                return LeavePlannerDayFlags.LeaveApproved;
            case LeavePlannerEntryKind.LeavePending:
                return LeavePlannerDayFlags.LeavePending;
            default:
                return LeavePlannerDayFlags.None;
        }
    }
}
