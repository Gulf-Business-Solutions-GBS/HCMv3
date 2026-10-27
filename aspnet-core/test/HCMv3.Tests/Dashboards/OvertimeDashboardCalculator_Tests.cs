using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Overtime;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Dashboards;

/// <summary>Counting rules of the Overtime Dashboard. Pure in-memory. Period: 1 Jan to 31 Mar 2026.</summary>
public class OvertimeDashboardCalculator_Tests
{
    private const int Normal = 1;
    private const int Weekend = 2;

    private static readonly DateTime Jan = new(2026, 1, 31);
    private static readonly DateTime Feb = new(2026, 2, 28);
    private static readonly DateTime Mar = new(2026, 3, 31);

    private readonly DashboardOrgTree _tree = new(new[]
    {
        new DashboardOrgUnit(1, null, "Company"),
        new DashboardOrgUnit(2, 1, "Finance"),
        new DashboardOrgUnit(3, 1, "Operations")
    });

    private readonly List<OvertimeEmployee> _employees = new()
    {
        new(1, "E1", "Amal", null, 2),
        new(2, "E2", "Badr", null, 2),
        new(3, "E3", "Chris", null, 3)
    };

    private OvertimeDashboardCalculator Calculator() => new(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31), _tree, _employees);

    private static OvertimeEntry Entry(int employee, DateTime month, int type, double approved, double claimed = -1) =>
        new(employee, month, type, claimed < 0 ? approved : claimed, approved, approved, null);

    private static OvertimeCost Cost(int employee, DateTime month, int type, double amount) => new(employee, month, type, amount);

    [Fact]
    public void Kpis_Should_Sum_Approved_Hours_And_Compare_Them_With_Claimed()
    {
        var entries = new[] { Entry(1, Jan, Normal, 8, claimed: 10), Entry(2, Jan, Weekend, 12), Entry(1, Feb, Normal, 0, claimed: 5) };
        var pending = new[] { new OvertimeEntry(3, Mar, Normal, 6, 6, 0, RequestStatus.Open) };

        var kpis = Calculator().Kpis(entries, new[] { Cost(1, Jan, Normal, 12.5), Cost(2, Jan, Weekend, 30.25) }, pending);

        kpis.ApprovedHours.ShouldBe(20);
        kpis.ClaimedHours.ShouldBe(27);
        kpis.ApprovedOfClaimedPercent.ShouldBe(74.1);
        kpis.Employees.ShouldBe(2); // employee 1's zero-approved February entry does not make a third
        kpis.AverageHoursPerEmployee.ShouldBe(10);
        kpis.Cost.ShouldBe(42.75);
        kpis.PendingEntries.ShouldBe(1);
        kpis.PendingHours.ShouldBe(6);
    }

    [Fact]
    public void Kpis_Should_Leave_Ratios_Empty_When_There_Is_No_Overtime()
    {
        var kpis = Calculator().Kpis(new List<OvertimeEntry>(), new List<OvertimeCost>(), new List<OvertimeEntry>());

        kpis.ApprovedOfClaimedPercent.ShouldBeNull();
        kpis.AverageHoursPerEmployee.ShouldBeNull();
    }

    [Fact]
    public void Trend_Should_Give_Every_Month_With_Hours_By_Type_And_Cost()
    {
        var trend = Calculator().Trend(
            new[] { Entry(1, Jan, Normal, 8), Entry(2, Jan, Weekend, 4), Entry(1, Mar, Normal, 2) },
            new[] { Cost(1, new DateTime(2026, 1, 31), Normal, 20) });

        trend.Select(p => p.MonthStart.Month).ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: false);
        trend[0].Hours.Select(v => (v.OvertimeTypeId, v.Hours)).ShouldBe(new[] { (Normal, 8.0), (Weekend, 4.0) }, ignoreOrder: false);
        trend[0].Cost.ShouldBe(20);
        trend[1].Hours.ShouldBeEmpty();
        trend[2].Hours.Single().Hours.ShouldBe(2);
    }

    [Fact]
    public void ByType_Should_Order_By_Hours_With_Each_Types_Cost()
    {
        var buckets = OvertimeDashboardCalculator.ByType(
            new[] { Entry(1, Jan, Normal, 3), Entry(2, Jan, Weekend, 9), Entry(3, Feb, Weekend, 1) },
            new[] { Cost(2, Jan, Weekend, 40) });

        buckets.Select(b => b.OvertimeTypeId).ShouldBe(new[] { Weekend, Normal }, ignoreOrder: false);
        buckets[0].Employees.ShouldBe(2);
        buckets[0].Cost.ShouldBe(40);
        buckets[1].Cost.ShouldBe(0);
    }

    [Fact]
    public void ByOrgUnit_Should_Put_Hours_And_Cost_In_The_Unit_One_Level_Down()
    {
        var buckets = Calculator().ByOrgUnit(
            new[] { Entry(1, Jan, Normal, 5), Entry(2, Jan, Normal, 5), Entry(3, Jan, Normal, 2) },
            new[] { Cost(1, Jan, Normal, 10), Cost(3, Jan, Normal, 4) },
            1);

        var finance = buckets.Single(b => b.OrganizationUnitId == 2);
        finance.Hours.ShouldBe(10);
        finance.Employees.ShouldBe(2);
        finance.Cost.ShouldBe(10);
        buckets[0].Name.ShouldBe("Finance");
        buckets.Single(b => b.OrganizationUnitId == 3).Cost.ShouldBe(4);
    }

    [Fact]
    public void TopEmployees_Should_Rank_By_Approved_Hours()
    {
        var top = Calculator().TopEmployees(
            new[] { Entry(1, Jan, Normal, 5), Entry(1, Feb, Weekend, 6), Entry(2, Jan, Normal, 7), Entry(3, Jan, Normal, 0, claimed: 9) },
            new[] { Cost(1, Jan, Normal, 15) });

        top.Select(t => (t.EmployeeCode, t.Hours)).ShouldBe(new[] { ("E1", 11.0), ("E2", 7.0) }, ignoreOrder: false);
        top[0].Cost.ShouldBe(15);
    }

    [Fact]
    public void ToRow_Should_Carry_The_Employees_Department()
    {
        var row = Calculator().ToRow(Entry(3, Feb, Weekend, 4, claimed: 6));

        row.OrganizationUnitName.ShouldBe("Operations");
        row.ClaimedHours.ShouldBe(6);
        row.ApprovedHours.ShouldBe(4);
    }
}
