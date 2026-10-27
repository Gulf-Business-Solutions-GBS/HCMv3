using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Workflow;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Dashboards;

/// <summary>Counting rules of the Workflow Requests Dashboard. Pure in-memory. Period: Q1 2026, "now" 31 Mar 2026 noon.</summary>
public class WorkflowDashboardCalculator_Tests
{
    private static readonly DateTime Now = new(2026, 3, 31, 12, 0, 0);

    private readonly DashboardOrgTree _tree = new(new[]
    {
        new DashboardOrgUnit(1, null, "Company"),
        new DashboardOrgUnit(2, 1, "Finance"),
        new DashboardOrgUnit(3, 1, "Operations")
    });

    private readonly List<WorkflowPerson> _people = new()
    {
        new(1, "E1", "Amal", null, 2),
        new(2, "E2", "Badr", null, 3),
        new(9, "M9", "Manager", null, null)
    };

    private WorkflowDashboardCalculator Calculator() => new(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31), Now, _tree, _people);

    private static int _nextId;

    private static WorkflowItem Item(RequestStatus status, DateTime created, int employee = 1, string type = "Leave",
        double? decidedAfterDays = null, DateTime? lastAction = null, int? dueInDays = null) =>
        new(++_nextId, "R" + _nextId, type, employee, created, status, 1,
            decidedAfterDays.HasValue ? created.AddDays(decidedAfterDays.Value) : null,
            lastAction ?? created, dueInDays);

    [Fact]
    public void Kpis_Should_Count_Every_Status_So_The_Parts_Add_Up()
    {
        var items = new[]
        {
            Item(RequestStatus.Open, new DateTime(2026, 3, 1)),
            Item(RequestStatus.Approved, new DateTime(2026, 1, 5), decidedAfterDays: 2),
            Item(RequestStatus.Approved, new DateTime(2026, 1, 6), decidedAfterDays: 4),
            Item(RequestStatus.Rejected, new DateTime(2026, 2, 1), decidedAfterDays: 6),
            Item(RequestStatus.Closed, new DateTime(2026, 2, 2)),
            Item(RequestStatus.Transferred, new DateTime(2026, 2, 3))
        };

        var kpis = Calculator().Kpis(items);

        kpis.Raised.ShouldBe(6);
        (kpis.Open + kpis.Approved + kpis.Rejected + kpis.Withdrawn + kpis.Transferred).ShouldBe(6);
        kpis.ApprovalRatePercent.ShouldBe(66.7);
        kpis.AverageDaysToDecide.ShouldBe(4);
        kpis.LongestWaitDays.ShouldBe(30.5);
    }

    [Fact]
    public void Kpis_Should_Leave_Ratios_Empty_When_Nothing_Was_Decided()
    {
        var kpis = Calculator().Kpis(new[] { Item(RequestStatus.Open, new DateTime(2026, 3, 30)) });

        kpis.ApprovalRatePercent.ShouldBeNull();
        kpis.AverageDaysToDecide.ShouldBeNull();
    }

    [Fact]
    public void IsOverdue_Should_Need_Due_Days_And_An_Open_Request()
    {
        var calculator = Calculator();

        calculator.IsOverdue(Item(RequestStatus.Open, new DateTime(2026, 3, 20), dueInDays: 5)).ShouldBeTrue();
        calculator.IsOverdue(Item(RequestStatus.Open, new DateTime(2026, 3, 30), dueInDays: 5)).ShouldBeFalse();
        calculator.IsOverdue(Item(RequestStatus.Open, new DateTime(2026, 1, 1), dueInDays: 0)).ShouldBeFalse(); // legacy: overdue
        calculator.IsOverdue(Item(RequestStatus.Open, new DateTime(2026, 1, 1))).ShouldBeFalse();
        calculator.IsOverdue(Item(RequestStatus.Approved, new DateTime(2026, 1, 1), decidedAfterDays: 20, dueInDays: 5)).ShouldBeFalse();
    }

    [Fact]
    public void DaysWaiting_Should_Run_From_The_Last_Action_Not_From_Raising()
    {
        var item = Item(RequestStatus.Open, new DateTime(2026, 3, 1), lastAction: new DateTime(2026, 3, 29, 12, 0, 0));

        Calculator().DaysWaiting(item).ShouldBe(2);
    }

    [Fact]
    public void Trend_Should_Give_Every_Month_With_Counts_By_Status()
    {
        var trend = Calculator().Trend(new[]
        {
            Item(RequestStatus.Open, new DateTime(2026, 1, 3)),
            Item(RequestStatus.Approved, new DateTime(2026, 1, 9), decidedAfterDays: 1),
            Item(RequestStatus.Approved, new DateTime(2026, 3, 9), decidedAfterDays: 1)
        });

        trend.Select(p => p.MonthStart.Month).ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: false);
        trend[0].Counts.Select(c => (c.Status, c.Count)).ShouldBe(new[] { (RequestStatus.Open, 1), (RequestStatus.Approved, 1) }, ignoreOrder: false);
        trend[1].Counts.ShouldBeEmpty();
    }

    [Fact]
    public void ByType_Should_Rank_Types_With_Their_Decisions()
    {
        var buckets = Calculator().ByType(new[]
        {
            Item(RequestStatus.Approved, new DateTime(2026, 1, 1), type: "Leave", decidedAfterDays: 3),
            Item(RequestStatus.Rejected, new DateTime(2026, 1, 1), type: "Leave", decidedAfterDays: 1),
            Item(RequestStatus.Open, new DateTime(2026, 3, 1), type: "Loan", dueInDays: 2)
        });

        buckets[0].RequestType.ShouldBe("Leave");
        buckets[0].AverageDaysToDecide.ShouldBe(2);
        buckets[1].Overdue.ShouldBe(1);
    }

    [Fact]
    public void ByOrgUnit_Should_Place_Requests_By_The_Requesters_Department()
    {
        var buckets = Calculator().ByOrgUnit(new[]
        {
            Item(RequestStatus.Open, new DateTime(2026, 3, 1), employee: 1),
            Item(RequestStatus.Open, new DateTime(2026, 3, 2), employee: 1),
            Item(RequestStatus.Approved, new DateTime(2026, 3, 3), employee: 2, decidedAfterDays: 1)
        }, 1);

        buckets[0].Name.ShouldBe("Finance");
        buckets[0].Open.ShouldBe(2);
        buckets.Single(b => b.OrganizationUnitId == 3).Raised.ShouldBe(1);
    }

    [Fact]
    public void Waiting_Should_Group_Open_Requests_By_Approver()
    {
        var open1 = Item(RequestStatus.Open, new DateTime(2026, 3, 1), dueInDays: 3);
        var open2 = Item(RequestStatus.Open, new DateTime(2026, 3, 29));
        var done = Item(RequestStatus.Approved, new DateTime(2026, 3, 1), decidedAfterDays: 1);

        var waiting = Calculator().Waiting(new[] { open1, open2, done },
            new[] { new WorkflowWaiting(open1.Id, 9), new WorkflowWaiting(open2.Id, 9), new WorkflowWaiting(done.Id, 9) });

        var manager = waiting.Single();
        manager.ApproverNameEn.ShouldBe("Manager");
        manager.Waiting.ShouldBe(2);
        manager.Overdue.ShouldBe(1);
        manager.OldestDays.ShouldBe(30.5);
    }

    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(2.9, 0)]
    [InlineData(3, 1)]
    [InlineData(7.99, 1)]
    [InlineData(14, 2)]
    [InlineData(30, 3)]
    [InlineData(31, 4)]
    public void BandOf_Should_Leave_No_Gaps(double days, int band)
    {
        WorkflowDashboardCalculator.BandOf(days).ShouldBe(band);
    }

    [Fact]
    public void WaitingBands_Should_Count_Only_Open_Requests_And_Keep_Every_Band()
    {
        var bands = Calculator().WaitingBands(new[]
        {
            Item(RequestStatus.Open, new DateTime(2026, 3, 30)),
            Item(RequestStatus.Open, new DateTime(2026, 1, 1)),
            Item(RequestStatus.Approved, new DateTime(2026, 1, 1), decidedAfterDays: 1)
        });

        bands.Count.ShouldBe(WorkflowDashboardCalculator.WaitingBandBounds.Length);
        bands[0].Requests.ShouldBe(1);
        bands.Last().Requests.ShouldBe(1);
        bands[1].ToDays.ShouldBe(7);
    }

    [Fact]
    public void ToRow_Should_Name_Who_It_Waits_With_And_When_It_Is_Due()
    {
        var item = Item(RequestStatus.Open, new DateTime(2026, 3, 20), dueInDays: 5);

        var row = Calculator().ToRow(item, new[] { 9 });

        row.WaitingWithEn.ShouldBe("Manager");
        row.DueDate.ShouldBe(new DateTime(2026, 3, 25));
        row.IsOverdue.ShouldBeTrue();
        row.OrganizationUnitName.ShouldBe("Finance");
    }
}
