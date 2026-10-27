using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Workforce;
using HCMv3.Dashboards.Workforce.Dto;
using HCMv3.GeneralMasterInfo;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Dashboards;

/// <summary>Movement and document rules of the Workforce Dashboard. Pure in-memory. Period: 1 Jan to 31 Mar 2026.</summary>
public class WorkforceActivityCalculator_Tests
{
    private static readonly DateTime From = new(2026, 1, 1);
    private static readonly DateTime AsOf = new(2026, 3, 31);

    private readonly DashboardOrgTree _tree = new(new[] { new DashboardOrgUnit(1, null, "Company"), new DashboardOrgUnit(2, 1, "Finance") });

    private static WorkforceEmployee Employee(int id, DateTime? lastDay = null) =>
        new(id, "E" + id, "Employee " + id, null, Gender.Male, new DateTime(1990, 1, 1), 1, null, null, 2,
            new DateTime(2020, 1, 1), lastDay == null, lastDay, null);

    private WorkforceActivityCalculator Calculator(params WorkforceEmployee[] population) =>
        new(From, AsOf, _tree, population, new[]
        {
            new WorkforceDocumentType(1, "Passport", null, 90),
            new WorkforceDocumentType(2, "Civil ID", null, 0)
        });

    private static readonly List<WorkforceHold> NoHolds = new();
    private static readonly List<WorkforceDatedEvent> None = new();
    private static readonly List<WorkforceSeparation> NoSeparations = new();

    [Fact]
    public void Movements_Should_Count_Holds_On_The_As_Of_Date_Only()
    {
        var rows = Calculator(Employee(1), Employee(2)).Movements(
            new[] { new WorkforceHold(1, new DateTime(2026, 3, 1), new DateTime(2026, 4, 30)), new WorkforceHold(2, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)) },
            None, NoSeparations, None);

        rows.Single().EmployeeCode.ShouldBe("E1");
        rows.Single().EndDate.ShouldBe(new DateTime(2026, 4, 30));
    }

    [Fact]
    public void Movements_Should_Count_Rejoins_Separations_And_Settlements_In_The_Period()
    {
        var rows = Calculator(Employee(1), Employee(2, new DateTime(2026, 2, 28)), Employee(3)).Movements(
            NoHolds,
            new[] { new WorkforceDatedEvent(1, new DateTime(2026, 2, 1)), new WorkforceDatedEvent(3, new DateTime(2025, 12, 1)) },
            new[] { new WorkforceSeparation(2, new DateTime(2026, 1, 31), new DateTime(2026, 2, 28)) },
            new[] { new WorkforceDatedEvent(2, new DateTime(2026, 3, 15)) });

        var counts = WorkforceActivityCalculator.Counts(rows);
        counts.Select(c => (c.Kind, c.Count)).ShouldBe(new[]
        {
            (WorkforceMovementKind.OnHold, 0),
            (WorkforceMovementKind.Rejoined, 1),
            (WorkforceMovementKind.Separated, 1),
            (WorkforceMovementKind.Settled, 1),
            (WorkforceMovementKind.AwaitingSettlement, 0)
        }, ignoreOrder: false);
    }

    [Fact]
    public void Awaiting_Settlement_Should_Include_Earlier_Leavers_But_Not_Those_Settled_Or_Rejoined()
    {
        var rows = Calculator(Employee(1, new DateTime(2025, 11, 30)), Employee(2, new DateTime(2025, 11, 30)), Employee(3, new DateTime(2025, 11, 30)), Employee(4)).Movements(
            NoHolds,
            new[] { new WorkforceDatedEvent(3, new DateTime(2026, 1, 10)) },
            new[]
            {
                new WorkforceSeparation(1, new DateTime(2025, 10, 1), new DateTime(2025, 11, 30)),
                new WorkforceSeparation(2, new DateTime(2025, 10, 1), new DateTime(2025, 11, 30)),
                new WorkforceSeparation(3, new DateTime(2025, 10, 1), new DateTime(2025, 11, 30)),
                new WorkforceSeparation(4, new DateTime(2026, 3, 1), new DateTime(2026, 4, 30)) // still serving notice
            },
            new[] { new WorkforceDatedEvent(2, new DateTime(2025, 12, 5)) });

        rows.Where(r => r.Kind == WorkforceMovementKind.AwaitingSettlement).Select(r => r.EmployeeCode).ShouldBe(new[] { "E1" });
    }

    [Fact]
    public void Movements_Should_Leave_Out_People_Outside_The_Population()
    {
        var rows = Calculator(Employee(1)).Movements(NoHolds, new[] { new WorkforceDatedEvent(9, new DateTime(2026, 2, 1)) }, NoSeparations, None);

        rows.ShouldBeEmpty();
    }

    [Fact]
    public void Documents_Should_Use_The_Types_Notice_Period_Or_Thirty_Days()
    {
        var calculator = Calculator(Employee(1), Employee(2));

        var rows = calculator.Documents(new[]
        {
            new WorkforceDocument(1, 1, "P1", AsOf.AddDays(80)),   // passport, 90 days: expiring
            new WorkforceDocument(1, 2, "C1", AsOf.AddDays(40)),   // civil ID, 30 days: not yet
            new WorkforceDocument(2, 2, "C2", AsOf.AddDays(30)),   // civil ID, on the edge: expiring
            new WorkforceDocument(2, 1, "P2", AsOf.AddDays(-3))    // expired
        });

        rows.Select(r => (r.DocumentNum, r.Status, r.DaysLeft)).ShouldBe(new[]
        {
            ("P2", WorkforceDocumentStatus.Expired, -3),
            ("C2", WorkforceDocumentStatus.Expiring, 30),
            ("P1", WorkforceDocumentStatus.Expiring, 80)
        }, ignoreOrder: false);
        calculator.NotificationDaysOf(2).ShouldBe(WorkforceActivityCalculator.DefaultNotificationDays);
    }

    [Fact]
    public void Documents_Should_Count_Only_The_Latest_Of_A_Type_And_Only_People_Employed_Today()
    {
        var rows = Calculator(Employee(1), Employee(2, new DateTime(2026, 2, 1))).Documents(new[]
        {
            new WorkforceDocument(1, 1, "OLD", AsOf.AddDays(-400)),
            new WorkforceDocument(1, 1, "NEW", AsOf.AddYears(4)),
            new WorkforceDocument(2, 1, "GONE", AsOf.AddDays(-5))
        });

        rows.ShouldBeEmpty();
    }

    [Fact]
    public void DocumentBuckets_Should_Split_Each_Type_Into_Expired_And_Expiring()
    {
        var calculator = Calculator(Employee(1), Employee(2));
        var rows = calculator.Documents(new[]
        {
            new WorkforceDocument(1, 1, "P1", AsOf.AddDays(10)),
            new WorkforceDocument(2, 1, "P2", AsOf.AddDays(-10)),
            new WorkforceDocument(2, 2, "C2", AsOf.AddDays(-1))
        });

        var buckets = calculator.DocumentBuckets(rows);

        buckets[0].NameEn.ShouldBe("Passport");
        (buckets[0].Expired, buckets[0].Expiring).ShouldBe((1, 1));
        buckets[0].NotificationDays.ShouldBe(90);
        buckets[1].Expired.ShouldBe(1);
    }
}
