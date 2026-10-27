using System;
using System.Linq;
using HCMv3.Dashboards;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// <see cref="LeavePlannerBuilder"/> -- the layout of the Leave Planner (legacy <c>uscLeavePlanner</c>), as an
/// employee-by-day grid for one month grouped by organization unit.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeavePlannerBuilder_Tests
{
    private static readonly DateTime November = new DateTime(2026, 11, 1);
    private static readonly DateTime NovemberEnd = new DateTime(2026, 11, 30);

    private static readonly DashboardOrgTree Tree = new DashboardOrgTree(new[]
    {
        new DashboardOrgUnit(1, null, "Head Office"),
        new DashboardOrgUnit(2, 1, "Finance"),
        new DashboardOrgUnit(3, 1, "Audit")
    });

    private static readonly LeavePlannerEmployee Ali = new LeavePlannerEmployee(10, "E010", "Ali", "علي", 2);
    private static readonly LeavePlannerEmployee Sara = new LeavePlannerEmployee(11, "E011", "Sara", "سارة", 2);
    private static readonly LeavePlannerEmployee Omar = new LeavePlannerEmployee(12, "E012", "Omar", "عمر", 3);
    private static readonly LeavePlannerEmployee Huda = new LeavePlannerEmployee(13, "E013", "Huda", "هدى", null);

    private static LeavePlannerDto Build(params LeavePlannerFact[] facts) =>
        LeavePlannerBuilder.Build(November, NovemberEnd, new[] { Ali, Sara, Omar, Huda }, facts, Tree, "No department");

    [Fact]
    public void Every_Row_Should_Have_One_Cell_Per_Day_Of_The_Month()
    {
        var planner = Build(new LeavePlannerFact(Ali.Id, LeavePlannerEntryKind.PlannedApproved, 1, new DateTime(2026, 11, 3), new DateTime(2026, 11, 4)));

        planner.Groups.Single().Rows.Single().Days.Count.ShouldBe(30);
    }

    [Fact]
    public void Only_Employees_With_Something_In_The_Month_Should_Get_A_Row()
    {
        // Legacy's scheduler showed appointments, not people: a unit of forty with two away is two rows.
        var planner = Build(new LeavePlannerFact(Ali.Id, LeavePlannerEntryKind.PlannedApproved, 1, new DateTime(2026, 11, 3), new DateTime(2026, 11, 4)));

        planner.Groups.SelectMany(g => g.Rows).Select(r => r.EmployeeId).ShouldBe(new[] { Ali.Id });
    }

    [Fact]
    public void An_Entry_Should_Be_Clipped_To_The_Month()
    {
        // 28 Oct - 2 Nov: only the 1st and 2nd are this month's business.
        var planner = Build(new LeavePlannerFact(Ali.Id, LeavePlannerEntryKind.LeaveApproved, 1, new DateTime(2026, 10, 28), new DateTime(2026, 11, 2)));

        var days = planner.Groups.Single().Rows.Single().Days;
        days[0].ShouldBe(LeavePlannerDayFlags.LeaveApproved);
        days[1].ShouldBe(LeavePlannerDayFlags.LeaveApproved);
        days[2].ShouldBe(LeavePlannerDayFlags.None);
    }

    [Fact]
    public void A_Plan_Over_Approved_Leave_Should_Show_Both_On_The_Day()
    {
        // The overlap is what the planner is for, so neither hides the other.
        var planner = Build(
            new LeavePlannerFact(Ali.Id, LeavePlannerEntryKind.LeaveApproved, 1, new DateTime(2026, 11, 5), new DateTime(2026, 11, 6), "Annual", "سنوية"),
            new LeavePlannerFact(Ali.Id, LeavePlannerEntryKind.PlannedPending, 2, new DateTime(2026, 11, 6), new DateTime(2026, 11, 8)));

        var row = planner.Groups.Single().Rows.Single();
        row.Days[4].ShouldBe(LeavePlannerDayFlags.LeaveApproved);
        row.Days[5].ShouldBe(LeavePlannerDayFlags.LeaveApproved | LeavePlannerDayFlags.PlannedPending);
        row.Days[6].ShouldBe(LeavePlannerDayFlags.PlannedPending);
        row.Entries.Count.ShouldBe(2);
    }

    [Fact]
    public void Rows_Should_Be_Grouped_By_Unit_With_The_Unplaced_Last()
    {
        var planner = Build(
            new LeavePlannerFact(Huda.Id, LeavePlannerEntryKind.PlannedApproved, 1, November, November),
            new LeavePlannerFact(Omar.Id, LeavePlannerEntryKind.PlannedApproved, 2, November, November),
            new LeavePlannerFact(Sara.Id, LeavePlannerEntryKind.PlannedApproved, 3, November, November),
            new LeavePlannerFact(Ali.Id, LeavePlannerEntryKind.PlannedApproved, 4, November, November));

        planner.Groups.Select(g => g.OrganizationUnitName).ShouldBe(new[] { "Audit", "Finance", "No department" });
        planner.Groups[1].Rows.Select(r => r.EmployeeCode).ShouldBe(new[] { "E010", "E011" });
    }

    [Fact]
    public void Away_Count_Should_Count_Employees_Not_Entries()
    {
        // Ali has a plan and a leave on the 5th; that is one person away, not two.
        var planner = Build(
            new LeavePlannerFact(Ali.Id, LeavePlannerEntryKind.LeaveApproved, 1, new DateTime(2026, 11, 5), new DateTime(2026, 11, 5)),
            new LeavePlannerFact(Ali.Id, LeavePlannerEntryKind.PlannedApproved, 2, new DateTime(2026, 11, 5), new DateTime(2026, 11, 5)),
            new LeavePlannerFact(Sara.Id, LeavePlannerEntryKind.PlannedApproved, 3, new DateTime(2026, 11, 5), new DateTime(2026, 11, 6)));

        var finance = planner.Groups.Single();
        finance.AwayCountByDay[4].ShouldBe(2);
        finance.AwayCountByDay[5].ShouldBe(1);
        finance.AwayCountByDay[6].ShouldBe(0);
    }

    [Fact]
    public void An_Entry_Outside_The_Month_Should_Be_Ignored()
    {
        var planner = Build(new LeavePlannerFact(Ali.Id, LeavePlannerEntryKind.PlannedApproved, 1, new DateTime(2026, 12, 1), new DateTime(2026, 12, 3)));

        planner.Groups.ShouldBeEmpty();
    }

    [Fact]
    public void An_Entry_For_An_Employee_Out_Of_Scope_Should_Be_Ignored()
    {
        // The reader passes only in-scope employees; a fact for anybody else must not invent a row.
        var planner = Build(new LeavePlannerFact(999, LeavePlannerEntryKind.PlannedApproved, 1, November, November));

        planner.Groups.ShouldBeEmpty();
    }

    [Fact]
    public void Month_Of_Should_Cover_The_Whole_Calendar_Month()
    {
        var (from, to) = LeavePlannerBuilder.MonthOf(new DateTime(2028, 2, 17));

        from.ShouldBe(new DateTime(2028, 2, 1));
        to.ShouldBe(new DateTime(2028, 2, 29));
    }
}
