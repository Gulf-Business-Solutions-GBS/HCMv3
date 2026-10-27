using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Workforce;
using HCMv3.Dashboards.Workforce.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// Counting rules of the Workforce Dashboard. Pure in-memory: no ABP module, no database.
/// Period used throughout: 1 Jan 2026 to 30 Jun 2026 (the as-of date).
/// </summary>
public class WorkforceDashboardCalculator_Tests
{
    private static readonly DateTime From = new(2026, 1, 1);
    private static readonly DateTime AsOf = new(2026, 6, 30);

    private const int Kuwaiti = 1;
    private const int Indian = 2;

    private readonly DashboardOrgTree _tree = new(new[]
    {
        new DashboardOrgUnit(1, null, "Company"),
        new DashboardOrgUnit(2, 1, "Finance"),
        new DashboardOrgUnit(3, 1, "Operations"),
        new DashboardOrgUnit(4, 2, "Payroll")
    });

    private WorkforceDashboardCalculator Calculator() => new(
        From,
        AsOf,
        _tree,
        new[] { new WorkforceLookup(Kuwaiti, "Kuwaiti", "كويتي"), new WorkforceLookup(Indian, "Indian", "هندي") },
        new[] { new WorkforceLookup(7, "Bachelor", "بكالوريوس") },
        new[] { new WorkforceLookup(9, "Accountant", "محاسب") });

    private static int _nextId = 1;

    private static WorkforceEmployee Employee(
        DateTime? joined = null,
        DateTime? birth = null,
        Gender gender = Gender.Male,
        int nationality = Kuwaiti,
        long? unit = 2,
        bool active = true,
        DateTime? lastDay = null,
        SeparationType? type = null,
        int? qualification = null,
        int? jobTitle = null)
    {
        var id = _nextId++;
        return new WorkforceEmployee(
            id, "E" + id, "Employee " + id, null, gender, birth ?? new DateTime(1990, 1, 1), nationality, qualification,
            jobTitle, unit, joined ?? new DateTime(2020, 1, 1), active, lastDay, type);
    }

    // ---------------------------------------------------------------- who is employed

    [Fact]
    public void IsEmployedOn_Should_Count_Someone_Up_To_And_Including_Their_Last_Day()
    {
        var leaver = Employee(active: false, lastDay: new DateTime(2026, 3, 15));

        WorkforceDashboardCalculator.IsEmployedOn(leaver, new DateTime(2026, 3, 15)).ShouldBeTrue();
        WorkforceDashboardCalculator.IsEmployedOn(leaver, new DateTime(2026, 3, 16)).ShouldBeFalse();
    }

    [Fact]
    public void IsEmployedOn_Should_Not_Count_Anyone_Before_They_Join()
    {
        var joiner = Employee(joined: new DateTime(2026, 4, 1));

        WorkforceDashboardCalculator.IsEmployedOn(joiner, new DateTime(2026, 3, 31)).ShouldBeFalse();
        WorkforceDashboardCalculator.IsEmployedOn(joiner, new DateTime(2026, 4, 1)).ShouldBeTrue();
    }

    /// <summary>User decision 2026-09-30: separations are the only source of exits. With no separation there is
    /// no exit date, so an inactive employee is left out of every count rather than guessed at.</summary>
    [Fact]
    public void IsEmployedOn_Should_Leave_Out_An_Inactive_Employee_With_No_Separation()
    {
        var inactive = Employee(active: false);

        WorkforceDashboardCalculator.IsEmployedOn(inactive, new DateTime(2021, 1, 1)).ShouldBeFalse();
        Calculator().LeftInPeriod(inactive).ShouldBeFalse();
    }

    [Fact]
    public void IsEmployedOn_Should_Keep_Counting_An_Active_Employee_Whose_Last_Day_Is_Still_Ahead()
    {
        var leaving = Employee(lastDay: new DateTime(2026, 12, 31));

        WorkforceDashboardCalculator.IsEmployedOn(leaving, AsOf).ShouldBeTrue();
    }

    // ---------------------------------------------------------------- age and service

    [Fact]
    public void CompletedYears_Should_Not_Count_A_Birthday_That_Has_Not_Come_Yet()
    {
        WorkforceDashboardCalculator.CompletedYears(new DateTime(1990, 7, 1), AsOf).ShouldBe(35);
        WorkforceDashboardCalculator.CompletedYears(new DateTime(1990, 6, 30), AsOf).ShouldBe(36);
    }

    [Fact]
    public void AgeBandOf_Should_Include_The_Under_21s_Legacy_Dropped()
    {
        var calculator = Calculator();

        calculator.AgeBandOf(Employee(birth: new DateTime(2006, 1, 1))).ShouldBe(WorkforceAgeBand.Under21);
        calculator.AgeBandOf(Employee(birth: new DateTime(2005, 6, 30))).ShouldBe(WorkforceAgeBand.From21To30);
        calculator.AgeBandOf(Employee(birth: new DateTime(1975, 6, 30))).ShouldBe(WorkforceAgeBand.Above50);
        calculator.AgeBandOf(Employee(birth: new DateTime(1, 1, 1))).ShouldBe(WorkforceAgeBand.Unknown);
    }

    [Fact]
    public void ServiceBandOf_Should_Use_Legacys_Groups()
    {
        var calculator = Calculator();

        calculator.ServiceBandOf(Employee(joined: new DateTime(2021, 7, 1))).ShouldBe(WorkforceServiceBand.From0To4);
        calculator.ServiceBandOf(Employee(joined: new DateTime(2021, 6, 30))).ShouldBe(WorkforceServiceBand.From5To10);
        calculator.ServiceBandOf(Employee(joined: new DateTime(2006, 6, 30))).ShouldBe(WorkforceServiceBand.From20);
    }

    // ---------------------------------------------------------------- headline figures

    [Fact]
    public void Kpis_Should_Compute_Turnover_As_Leavers_Over_Average_Headcount()
    {
        // 10 employed all period, 2 of them leave in March -> headcount 10 at start, 8 at end, average 9.
        var population = Enumerable.Range(0, 8).Select(_ => Employee()).ToList();
        population.Add(Employee(active: false, lastDay: new DateTime(2026, 3, 10), type: SeparationType.Resignation));
        population.Add(Employee(active: false, lastDay: new DateTime(2026, 3, 20), type: SeparationType.Termination));

        var kpis = Calculator().Kpis(population);

        kpis.Headcount.ShouldBe(8);
        kpis.Leavers.ShouldBe(2);
        kpis.Resignations.ShouldBe(1);
        kpis.Terminations.ShouldBe(1);
        kpis.AverageHeadcount.ShouldBe(9);
        kpis.TurnoverPercent.ShouldBe(22.2);
    }

    [Fact]
    public void Kpis_Should_Count_Joiners_And_Leave_Turnover_Empty_When_There_Is_Nobody()
    {
        var calculator = Calculator();

        calculator.Kpis(new List<WorkforceEmployee>()).TurnoverPercent.ShouldBeNull();

        var kpis = calculator.Kpis(new[]
        {
            Employee(joined: new DateTime(2026, 2, 1)),
            Employee(joined: new DateTime(2025, 12, 31)),
            Employee(joined: new DateTime(2026, 7, 1)) // after the as-of date
        });

        kpis.Joiners.ShouldBe(1);
        kpis.Headcount.ShouldBe(2);
    }

    [Fact]
    public void Kpis_Should_Split_Gender_And_Average_Age_And_Service()
    {
        var kpis = Calculator().Kpis(new[]
        {
            Employee(gender: Gender.Male, birth: new DateTime(1986, 1, 1), joined: new DateTime(2016, 1, 1)),   // 40, 10 yrs
            Employee(gender: Gender.Female, birth: new DateTime(1996, 1, 1), joined: new DateTime(2024, 1, 1)), // 30, 2 yrs
            Employee(gender: Gender.NotSpecified, birth: new DateTime(1, 1, 1), nationality: Indian)          // age unknown
        });

        kpis.Male.ShouldBe(1);
        kpis.Female.ShouldBe(1);
        kpis.GenderNotSpecified.ShouldBe(1);
        kpis.AverageAge.ShouldBe(35);
        kpis.Nationalities.ShouldBe(2);
    }

    // ---------------------------------------------------------------- trend

    [Fact]
    public void Trend_Should_Give_One_Point_Per_Month_With_Its_Own_Turnover()
    {
        var population = Enumerable.Range(0, 4).Select(_ => Employee()).ToList();
        population.Add(Employee(active: false, lastDay: new DateTime(2026, 3, 10), type: SeparationType.Resignation));
        population.Add(Employee(joined: new DateTime(2026, 5, 4)));

        var trend = Calculator().Trend(population);

        trend.Count.ShouldBe(6);
        trend[2].MonthStart.ShouldBe(new DateTime(2026, 3, 1));
        trend[2].Leavers.ShouldBe(1);
        trend[2].Headcount.ShouldBe(4);
        trend[2].TurnoverPercent.ShouldBe(22.2); // 1 / ((5 + 4) / 2)
        trend[4].Joiners.ShouldBe(1);
        trend[0].TurnoverPercent.ShouldBe(0);
    }

    // ---------------------------------------------------------------- distributions

    [Fact]
    public void AgeBands_Should_List_Every_Band_And_Unknown_Only_When_Used()
    {
        var calculator = Calculator();

        var bands = calculator.AgeBands(new[] { Employee(birth: new DateTime(1990, 1, 1)) });
        bands.Select(b => (WorkforceAgeBand)b.Band).ShouldBe(new[]
        {
            WorkforceAgeBand.Under21, WorkforceAgeBand.From21To30, WorkforceAgeBand.From31To40,
            WorkforceAgeBand.From41To50, WorkforceAgeBand.Above50
        }, ignoreOrder: false);
        bands.Single(b => b.Band == (int)WorkforceAgeBand.From31To40).Count.ShouldBe(1);

        calculator.AgeBands(new[] { Employee(birth: new DateTime(1, 1, 1)) })
            .ShouldContain(b => b.Band == (int)WorkforceAgeBand.Unknown && b.Count == 1);
    }

    [Fact]
    public void Nationalities_Should_Roll_Everything_Past_The_Top_Ten_Into_Others()
    {
        var headcount = new List<WorkforceEmployee>();
        for (var n = 100; n < 113; n++)
        {
            headcount.Add(Employee(nationality: n));
        }

        headcount.Add(Employee(nationality: 100)); // the biggest group

        var bars = Calculator().Nationalities(headcount);

        bars.Count.ShouldBe(WorkforceDashboardCalculator.TopCount + 1);
        bars[0].Id.ShouldBe(100);
        bars[0].Count.ShouldBe(2);
        bars.Last().IsOthers.ShouldBeTrue();
        bars.Last().Count.ShouldBe(3);
    }

    [Fact]
    public void Qualifications_Should_Keep_Not_Recorded_Visible_As_Id_Zero()
    {
        var bars = Calculator().Qualifications(new[] { Employee(qualification: 7), Employee(), Employee() });

        bars[0].Id.ShouldBe(0);
        bars[0].Count.ShouldBe(2);
        bars[1].NameEn.ShouldBe("Bachelor");
    }

    [Fact]
    public void ApplyAttributeFilters_Should_Narrow_By_Every_Clicked_Bar_Together()
    {
        var match = Employee(gender: Gender.Female, birth: new DateTime(1990, 1, 1), jobTitle: 9);
        var population = new[]
        {
            match,
            Employee(gender: Gender.Male, birth: new DateTime(1990, 1, 1), jobTitle: 9),
            Employee(gender: Gender.Female, birth: new DateTime(1970, 1, 1), jobTitle: 9),
            Employee(gender: Gender.Female, birth: new DateTime(1990, 1, 1))
        };

        var filtered = Calculator().ApplyAttributeFilters(population, new WorkforceDashboardInput
        {
            Gender = Gender.Female,
            AgeBand = WorkforceAgeBand.From31To40,
            JobTitleId = 9
        });

        filtered.ShouldBe(new[] { match });
    }

    [Fact]
    public void ByOrgUnit_Should_Give_Headcount_Leavers_And_Turnover_Per_Unit()
    {
        var population = new[]
        {
            Employee(unit: 2),
            Employee(unit: 4),
            Employee(unit: 4, active: false, lastDay: new DateTime(2026, 2, 1), type: SeparationType.Resignation),
            Employee(unit: 3)
        };

        var buckets = Calculator().ByOrgUnit(population, 1);

        var finance = buckets.Single(b => b.OrganizationUnitId == 2);
        finance.Headcount.ShouldBe(2);
        finance.Leavers.ShouldBe(1);
        finance.TurnoverPercent.ShouldBe(40); // 1 / ((3 + 2) / 2)
        finance.HasChildren.ShouldBeTrue();
        buckets.Single(b => b.OrganizationUnitId == 3).Headcount.ShouldBe(1);
    }

    [Fact]
    public void Select_Should_Pick_The_Employees_Behind_Each_Figure()
    {
        var stayer = Employee();
        var joiner = Employee(joined: new DateTime(2026, 2, 1));
        var leaver = Employee(active: false, lastDay: new DateTime(2026, 2, 1), type: SeparationType.Termination);
        var population = new[] { stayer, joiner, leaver };
        var calculator = Calculator();

        calculator.Select(population, WorkforceEmployeeListKind.Headcount).ShouldBe(new[] { stayer, joiner });
        calculator.Select(population, WorkforceEmployeeListKind.Joiners).ShouldBe(new[] { joiner });
        calculator.Select(population, WorkforceEmployeeListKind.Leavers).ShouldBe(new[] { leaver });
    }
}
