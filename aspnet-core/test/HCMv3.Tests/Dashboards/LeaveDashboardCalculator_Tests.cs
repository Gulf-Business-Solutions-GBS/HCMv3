using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Leave;
using HCMv3.Dashboards.Leave.Dto;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// Counting rules of the Leave Dashboard. Pure in-memory: no ABP module, no database.
///
/// Organisation used throughout:
/// <code>
/// 1 Company
/// ├── 2 Finance
/// │   └── 4 Payroll
/// └── 3 Operations
/// 9 Other Company (no employees)
/// </code>
/// </summary>
public class LeaveDashboardCalculator_Tests
{
    private const int Annual = 1;   // requires a return date
    private const int Sick = 2;     // does not

    private static readonly DateTime Today = new DateTime(2026, 3, 15);

    private readonly List<LeaveDashboardEmployee> _employees = new()
    {
        new(1, "E1", "Amal", "أمل", true, 2),
        new(2, "E2", "Badr", "بدر", true, 4),
        new(3, "E3", "Chris", "كريس", true, 3),
        new(4, "E4", "Dana", "دانا", false, 3),   // inactive
        new(5, "E5", "Eid", "عيد", true, null),     // no assignment
        new(6, "E6", "Fahad", "فهد", true, 1)       // sits directly in the company
    };

    private readonly List<DashboardOrgUnit> _units = new()
    {
        new(1, null, "Company"),
        new(2, 1, "Finance"),
        new(3, 1, "Operations"),
        new(4, 2, "Payroll"),
        new(9, null, "Other Company")
    };

    private readonly List<LeaveDashboardAbsenceType> _types = new()
    {
        new(Annual, "Annual", "سنوية"),
        new(Sick, "Sick", "مرضية")
    };

    private LeaveDashboardCalculator Calculator(DateTime? from = null, DateTime? to = null) =>
        new(Today, from ?? new DateTime(2026, 1, 1), to ?? new DateTime(2026, 12, 31), _employees, _units, _types);

    private static int _nextId = 100;

    private static LeaveDashboardLeaveFact Leave(
        int employeeId,
        DateTime start,
        DateTime end,
        int type = Annual,
        double days = 0,
        bool isReturned = false,
        DateTime returnDate = default,
        RequestStatus? status = null) =>
        new(_nextId++, employeeId, type, start, end, days, isReturned, returnDate, type == Annual, status);

    // ---------------------------------------------------------------- today

    [Fact]
    public void OnLeaveToday_Should_Include_Leave_Covering_Today_Including_Its_Last_Day()
    {
        var leaves = new[]
        {
            Leave(1, Today.AddDays(-3), Today.AddDays(2)),
            Leave(2, Today.AddDays(-3), Today),              // last day is today: still out today
            Leave(3, Today.AddDays(1), Today.AddDays(4)),    // starts tomorrow
            Leave(5, Today.AddDays(-9), Today.AddDays(-1))   // ended yesterday
        };

        var rows = Calculator().OnLeaveToday(leaves);

        rows.Select(r => r.EmployeeId).ShouldBe(new[] { 2, 1 }, ignoreOrder: false);
    }

    [Fact]
    public void OnLeaveToday_Should_Exclude_Returned_Leave_And_Inactive_Employees()
    {
        var leaves = new[]
        {
            Leave(1, Today.AddDays(-3), Today.AddDays(2), isReturned: true),
            Leave(4, Today.AddDays(-3), Today.AddDays(2))
        };

        Calculator().OnLeaveToday(leaves).ShouldBeEmpty();
    }

    [Fact]
    public void NotReturned_Should_Apply_The_Payroll_Rule_And_Put_The_Longest_Overdue_First()
    {
        var leaves = new[]
        {
            Leave(1, Today.AddDays(-20), Today.AddDays(-2)),
            Leave(2, Today.AddDays(-40), Today.AddDays(-10)),
            Leave(3, Today.AddDays(-5), Today.AddDays(-1), type: Sick),                       // type needs no return
            Leave(5, Today.AddDays(-5), Today.AddDays(-1), returnDate: Today.AddDays(-1)),  // return recorded
            Leave(6, Today.AddDays(-5), Today.AddDays(-1), isReturned: true),
            Leave(4, Today.AddDays(-5), Today.AddDays(-1))                                   // inactive
        };

        var rows = Calculator().NotReturned(leaves);

        rows.Select(r => r.EmployeeId).ShouldBe(new[] { 2, 1 }, ignoreOrder: false);
        rows[0].DaysOverdue.ShouldBe(10);
        rows[1].DaysOverdue.ShouldBe(2);
    }

    [Fact]
    public void StartingSoon_Should_Cover_Tomorrow_To_Seven_Days_Ahead()
    {
        var leaves = new[]
        {
            Leave(1, Today, Today.AddDays(3)),                    // already started
            Leave(2, Today.AddDays(1), Today.AddDays(3)),
            Leave(3, Today.AddDays(7), Today.AddDays(9)),
            Leave(5, Today.AddDays(8), Today.AddDays(9))          // too far
        };

        Calculator().StartingSoon(leaves).Select(r => r.EmployeeId).ShouldBe(new[] { 2, 3 }, ignoreOrder: false);
    }

    [Fact]
    public void ReturningSoon_Should_List_Leave_Ending_Within_The_Week_Of_Types_That_Expect_A_Return()
    {
        var leaves = new[]
        {
            Leave(1, Today.AddDays(-5), Today),                   // back tomorrow
            Leave(2, Today.AddDays(-5), Today.AddDays(6)),        // back in 7 days
            Leave(3, Today.AddDays(-5), Today.AddDays(7)),        // back in 8 days: too far
            Leave(5, Today.AddDays(-5), Today, type: Sick),       // no return expected
            Leave(6, Today.AddDays(-5), Today.AddDays(-1))        // already due: that is not-returned
        };

        Calculator().ReturningSoon(leaves).Select(r => r.EmployeeId).ShouldBe(new[] { 1, 2 }, ignoreOrder: false);
    }

    [Fact]
    public void PendingApproval_Should_List_Only_Open_Requests()
    {
        var leaves = new[]
        {
            Leave(1, Today.AddDays(5), Today.AddDays(6), status: RequestStatus.Open),
            Leave(2, Today.AddDays(5), Today.AddDays(6), status: RequestStatus.Draft),
            Leave(3, Today.AddDays(5), Today.AddDays(6), status: RequestStatus.Rejected)
        };

        Calculator().PendingApproval(leaves).Select(r => r.EmployeeId).ShouldBe(new[] { 1 });
    }

    // ---------------------------------------------------------------- leave days

    [Fact]
    public void LeaveDaysWithin_Should_Spread_Leave_Days_Evenly_Over_Calendar_Days()
    {
        // 14 calendar days, 10 leave days (weekends excluded), 7 calendar days on each side of 1 April.
        var leave = Leave(1, new DateTime(2026, 3, 25), new DateTime(2026, 4, 7), days: 10);

        LeaveDashboardCalculator.LeaveDaysWithin(leave, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)).ShouldBe(5);
        LeaveDashboardCalculator.LeaveDaysWithin(leave, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30)).ShouldBe(5);
        LeaveDashboardCalculator.LeaveDaysWithin(leave, new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)).ShouldBe(0);
    }

    [Fact]
    public void LeaveDaysWithin_Should_Fall_Back_To_Calendar_Days_When_The_Row_Has_No_Day_Count()
    {
        var leave = Leave(1, new DateTime(2026, 3, 30), new DateTime(2026, 4, 2), days: 0);

        LeaveDashboardCalculator.LeaveDaysWithin(leave, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)).ShouldBe(2);
    }

    [Fact]
    public void LeaveDaysInPeriod_Should_Count_Only_The_Share_Inside_The_Period()
    {
        var leaves = new[]
        {
            Leave(1, new DateTime(2025, 12, 29), new DateTime(2026, 1, 1), days: 4), // 1 of 4 days inside
            Leave(2, new DateTime(2026, 2, 1), new DateTime(2026, 2, 3), days: 3)
        };

        Calculator().LeaveDaysInPeriod(leaves).ShouldBe(4);
    }

    // ---------------------------------------------------------------- trend

    [Fact]
    public void Trend_Should_Be_Monthly_With_Every_Month_Present_For_A_Long_Period()
    {
        var calculator = Calculator(new DateTime(2026, 1, 1), new DateTime(2026, 6, 30));
        var leaves = new[]
        {
            Leave(1, new DateTime(2026, 3, 25), new DateTime(2026, 4, 7), days: 10),
            Leave(2, new DateTime(2026, 3, 2), new DateTime(2026, 3, 3), type: Sick, days: 2)
        };

        var trend = calculator.Trend(leaves);

        calculator.Granularity.ShouldBe(LeaveDashboardGranularity.Month);
        trend.Count.ShouldBe(6);
        trend[0].Values.ShouldBeEmpty();

        var march = trend[2];
        march.PeriodStart.ShouldBe(new DateTime(2026, 3, 1));
        march.Values.Single(v => v.AbsenceTypeId == Annual).Value.ShouldBe(5);
        march.Values.Single(v => v.AbsenceTypeId == Sick).Value.ShouldBe(2);

        trend[3].Values.Single().Value.ShouldBe(5);
    }

    [Fact]
    public void Trend_Should_Count_Employees_Out_Per_Day_For_A_Short_Period()
    {
        var calculator = Calculator(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));
        var leaves = new[]
        {
            Leave(1, new DateTime(2026, 3, 2), new DateTime(2026, 3, 4)),
            Leave(2, new DateTime(2026, 3, 4), new DateTime(2026, 3, 5))
        };

        var trend = calculator.Trend(leaves);

        calculator.Granularity.ShouldBe(LeaveDashboardGranularity.Day);
        trend.Count.ShouldBe(31);
        trend[3].PeriodStart.ShouldBe(new DateTime(2026, 3, 4));
        trend[3].Values.Single().Value.ShouldBe(2);
        trend[4].Values.Single().Value.ShouldBe(1);
        trend[5].Values.ShouldBeEmpty();
    }

    [Fact]
    public void Granularity_Should_Switch_To_Month_Above_Sixty_Two_Days()
    {
        Calculator(new DateTime(2026, 1, 1), new DateTime(2026, 3, 3)).Granularity.ShouldBe(LeaveDashboardGranularity.Day);
        Calculator(new DateTime(2026, 1, 1), new DateTime(2026, 3, 4)).Granularity.ShouldBe(LeaveDashboardGranularity.Month);
    }

    // ---------------------------------------------------------------- breakdowns

    [Fact]
    public void ByOrgUnit_Should_Group_By_Top_Level_Unit_With_Unassigned_Separate()
    {
        var leaves = new[]
        {
            Leave(1, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), days: 2),   // Finance -> Company
            Leave(2, new DateTime(2026, 2, 1), new DateTime(2026, 2, 3), days: 3),   // Payroll -> Company
            Leave(5, new DateTime(2026, 2, 1), new DateTime(2026, 2, 1), days: 1)    // unassigned
        };

        var buckets = Calculator().ByOrgUnit(leaves, null);

        buckets.Count.ShouldBe(2);
        buckets[0].OrganizationUnitId.ShouldBe(1);
        buckets[0].Employees.ShouldBe(2);
        buckets[0].LeaveDays.ShouldBe(5);
        buckets[0].HasChildren.ShouldBeTrue();
        buckets[1].OrganizationUnitId.ShouldBeNull();
    }

    [Fact]
    public void ByOrgUnit_Should_Drill_One_Level_Down_And_Keep_Direct_Staff_Apart()
    {
        var leaves = new[]
        {
            Leave(1, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), days: 2),   // Finance
            Leave(2, new DateTime(2026, 2, 1), new DateTime(2026, 2, 3), days: 3),   // Payroll -> Finance
            Leave(3, new DateTime(2026, 2, 1), new DateTime(2026, 2, 1), days: 1),   // Operations
            Leave(6, new DateTime(2026, 2, 1), new DateTime(2026, 2, 4), days: 4),   // Company itself
            Leave(5, new DateTime(2026, 2, 1), new DateTime(2026, 2, 1), days: 1)    // unassigned: not under Company
        };

        var buckets = Calculator().ByOrgUnit(leaves, 1);

        buckets.Select(b => (b.OrganizationUnitId, b.IsDirectStaff, b.LeaveDays)).ShouldBe(new (long?, bool, double)[]
        {
            (2, false, 5),
            (1, true, 4),
            (3, false, 1)
        }, ignoreOrder: false);
        buckets.Single(b => b.IsDirectStaff).HasChildren.ShouldBeFalse();
    }

    [Fact]
    public void ByType_Should_Order_By_Leave_Days()
    {
        var leaves = new[]
        {
            Leave(1, new DateTime(2026, 2, 1), new DateTime(2026, 2, 2), days: 2),
            Leave(2, new DateTime(2026, 2, 1), new DateTime(2026, 2, 5), type: Sick, days: 5)
        };

        var buckets = Calculator().ByType(leaves);

        buckets.Select(b => b.NameEn).ShouldBe(new[] { "Sick", "Annual" }, ignoreOrder: false);
    }

    // ---------------------------------------------------------------- tree

    [Fact]
    public void SelfAndDescendants_Should_Walk_The_Whole_Subtree()
    {
        Calculator().SelfAndDescendants(1).ShouldBe(new long[] { 1, 2, 3, 4 }, ignoreOrder: true);
        Calculator().SelfAndDescendants(2).ShouldBe(new long[] { 2, 4 }, ignoreOrder: true);
    }

    [Fact]
    public void OrgUnitOptions_Should_Offer_Only_Units_Holding_Employees_Plus_Their_Ancestors()
    {
        var options = Calculator().OrgUnitOptions(_employees.Where(e => e.Id == 2));

        options.Select(o => (o.Name, o.Depth)).ShouldBe(new[]
        {
            ("Company", 0),
            ("Finance", 1),
            ("Payroll", 2)
        }, ignoreOrder: false);
    }

    [Fact]
    public void PathTo_Should_Run_From_The_Top_Down()
    {
        Calculator().PathTo(4).Select(p => p.Name).ShouldBe(new[] { "Company", "Finance", "Payroll" }, ignoreOrder: false);
        Calculator().PathTo(null).ShouldBeEmpty();
    }
}
