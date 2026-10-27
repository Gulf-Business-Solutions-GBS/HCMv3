using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards;
using HCMv3.Dashboards.Attendance;
using HCMv3.Dashboards.Attendance.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Dashboards;

/// <summary>
/// Counting rules of the Time &amp; Attendance Dashboard. Pure in-memory: no ABP module, no database.
/// Period: 1 Mar to 31 Mar 2026 (daily), today 20 Mar 2026. 1 Mar 2026 is a Sunday.
/// </summary>
public class AttendanceDashboardCalculator_Tests
{
    private static readonly DateTime From = new(2026, 3, 1);
    private static readonly DateTime To = new(2026, 3, 31);
    private static readonly DateTime Today = new(2026, 3, 20);

    private readonly DashboardOrgTree _tree = new(new[]
    {
        new DashboardOrgUnit(1, null, "Company"),
        new DashboardOrgUnit(2, 1, "Finance"),
        new DashboardOrgUnit(3, 1, "Operations")
    });

    private readonly List<AttendanceEmployee> _employees = new()
    {
        new(1, "E1", "Amal", null, 2),
        new(2, "E2", "Badr", null, 2),
        new(3, "E3", "Chris", null, 3)
    };

    private AttendanceDashboardCalculator Calculator(DateTime? from = null, DateTime? to = null) =>
        new(from ?? From, to ?? To, Today, _tree, _employees);

    private static AttendanceDay Day(
        int employee,
        int dayOfMonth,
        bool planned = true,
        bool leave = false,
        bool off = false,
        bool absent = false,
        double late = 0,
        double early = 0,
        bool punchIn = true,
        bool punchOut = true)
    {
        var date = new DateTime(2026, 3, dayOfMonth);
        return new AttendanceDay(
            employee, date, planned, leave, off, absent, late, early,
            date.AddHours(8), date.AddHours(16),
            punchIn ? date.AddHours(8) : null, punchOut ? date.AddHours(16) : null,
            planned ? 480 : 0, planned && !absent ? 480 : 0);
    }

    // ---------------------------------------------------------------- rules

    [Fact]
    public void A_Working_Day_Should_Exclude_Leave_Holidays_And_Days_Off()
    {
        AttendanceDashboardCalculator.IsWorkingDay(Day(1, 2)).ShouldBeTrue();
        AttendanceDashboardCalculator.IsWorkingDay(Day(1, 2, leave: true)).ShouldBeFalse();
        AttendanceDashboardCalculator.IsWorkingDay(Day(1, 2, off: true)).ShouldBeFalse();
        AttendanceDashboardCalculator.IsWorkingDay(Day(1, 2, planned: false)).ShouldBeFalse();
    }

    [Fact]
    public void Lateness_Should_Only_Count_On_A_Present_Day()
    {
        AttendanceDashboardCalculator.IsLate(Day(1, 2, late: 15)).ShouldBeTrue();
        AttendanceDashboardCalculator.IsLate(Day(1, 2, late: 15, absent: true)).ShouldBeFalse();
        AttendanceDashboardCalculator.IsLate(Day(1, 2, late: 15, leave: true)).ShouldBeFalse();
    }

    [Fact]
    public void A_Missing_Out_Punch_Should_Not_Count_For_Today()
    {
        var calculator = Calculator();

        calculator.IsMissingPunch(Day(1, 19, punchOut: false)).ShouldBeTrue();
        calculator.IsMissingPunch(Day(1, 20, punchOut: false)).ShouldBeFalse(); // today: may not have left yet
        calculator.IsMissingPunch(Day(1, 20, punchIn: false)).ShouldBeTrue();  // no in-punch counts any day
        calculator.IsMissingPunch(Day(1, 19, punchIn: false, punchOut: false)).ShouldBeFalse(); // no punch at all
    }

    // ---------------------------------------------------------------- figures

    [Fact]
    public void Kpis_Should_Rate_Attendance_Over_Working_Days()
    {
        var days = new List<AttendanceDay>
        {
            Day(1, 2), Day(1, 3, late: 30), Day(1, 4, absent: true), Day(1, 5, early: 90),
            Day(1, 6, leave: true), Day(1, 7, off: true)
        };

        var kpis = Calculator().Kpis(days);

        kpis.WorkingDays.ShouldBe(4);
        kpis.AbsentDays.ShouldBe(1);
        kpis.PresentDays.ShouldBe(3);
        kpis.AttendanceRatePercent.ShouldBe(75);
        kpis.LateDays.ShouldBe(1);
        kpis.LateHours.ShouldBe(0.5);
        kpis.EarlyDays.ShouldBe(1);
        kpis.EarlyHours.ShouldBe(1.5);
        kpis.PlannedHours.ShouldBe(32);
        kpis.ActualHours.ShouldBe(24);
    }

    [Fact]
    public void Kpis_Should_Leave_The_Rate_Empty_When_Nobody_Was_Due_To_Work()
    {
        Calculator().Kpis(new List<AttendanceDay> { Day(1, 7, off: true) }).AttendanceRatePercent.ShouldBeNull();
    }

    [Fact]
    public void Trend_Should_Give_Every_Day_Of_A_Short_Period()
    {
        var calculator = Calculator();
        var trend = calculator.Trend(new List<AttendanceDay> { Day(1, 3, absent: true), Day(2, 3), Day(1, 4, late: 5) });

        calculator.Granularity.ShouldBe(AttendanceDashboardGranularity.Day);
        trend.Count.ShouldBe(31);
        trend[2].AbsentDays.ShouldBe(1);
        trend[2].AttendanceRatePercent.ShouldBe(50);
        trend[3].LateDays.ShouldBe(1);
        trend[0].AttendanceRatePercent.ShouldBeNull();
    }

    [Fact]
    public void Trend_Should_Go_Monthly_For_A_Long_Period()
    {
        var calculator = Calculator(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));
        var trend = calculator.Trend(new List<AttendanceDay> { Day(1, 3, absent: true) });

        calculator.Granularity.ShouldBe(AttendanceDashboardGranularity.Month);
        trend.Select(p => p.PeriodStart.Month).ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: false);
        trend[2].AbsentDays.ShouldBe(1);
    }

    [Fact]
    public void ByOrgUnit_Should_Put_The_Lowest_Attendance_First()
    {
        var days = new List<AttendanceDay>
        {
            Day(1, 2), Day(2, 2, absent: true), // Finance: 1 of 2
            Day(3, 2), Day(3, 3)                  // Operations: 2 of 2
        };

        var buckets = Calculator().ByOrgUnit(days, null);
        buckets.Single().OrganizationUnitId.ShouldBe(1); // top level: all under Company

        var below = Calculator().ByOrgUnit(days, 1);
        below.Select(b => (b.Name, b.AttendanceRatePercent)).ShouldBe(new (string, double?)[]
        {
            ("Finance", 50),
            ("Operations", 100)
        }, ignoreOrder: false);
    }

    [Fact]
    public void Top_Charts_Should_Rank_Employees_By_Days_Or_Hours()
    {
        var days = new List<AttendanceDay>
        {
            Day(1, 2, absent: true), Day(1, 3, absent: true), Day(2, 2, absent: true),
            Day(2, 4, late: 90), Day(3, 4, late: 30), Day(3, 5, early: 60)
        };
        var calculator = Calculator();

        calculator.TopAbsent(days).Select(t => (t.EmployeeCode, t.Value)).ShouldBe(new[] { ("E1", 2.0), ("E2", 1.0) }, ignoreOrder: false);
        calculator.TopLate(days).Select(t => (t.EmployeeCode, t.Value)).ShouldBe(new[] { ("E2", 1.5), ("E3", 0.5) }, ignoreOrder: false);
        calculator.TopEarly(days).Single().Value.ShouldBe(1);
    }

    [Fact]
    public void Weekdays_Should_Count_Late_And_Absent_Days_Per_Day_Of_The_Week()
    {
        // 2 Mar 2026 is a Monday; 9 Mar also.
        var weekdays = AttendanceDashboardCalculator.Weekdays(new List<AttendanceDay>
        {
            Day(1, 2, late: 10), Day(2, 9, late: 5), Day(3, 3, absent: true)
        });

        weekdays.Count.ShouldBe(7);
        weekdays[(int)DayOfWeek.Monday].LateDays.ShouldBe(2);
        weekdays[(int)DayOfWeek.Tuesday].AbsentDays.ShouldBe(1);
    }

    [Fact]
    public void Matches_Should_Pick_The_Days_Behind_Each_Figure()
    {
        var calculator = Calculator();
        var late = Day(1, 2, late: 10);
        var absent = Day(1, 3, absent: true);
        var normal = Day(1, 4);

        calculator.Matches(late, AttendanceExceptionKind.Late).ShouldBeTrue();
        calculator.Matches(absent, AttendanceExceptionKind.Late).ShouldBeFalse();
        calculator.Matches(absent, AttendanceExceptionKind.Any).ShouldBeTrue();
        calculator.Matches(normal, AttendanceExceptionKind.Any).ShouldBeFalse();
        calculator.ToRow(late).OrganizationUnitName.ShouldBe("Finance");
    }
}
