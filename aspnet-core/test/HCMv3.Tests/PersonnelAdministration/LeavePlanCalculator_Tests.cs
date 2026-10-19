using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using NSubstitute;
using Shouldly;
using Xunit;
using static HCMv3.Tests.PayrollManagement.PayrollEngineFakes;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// <see cref="LeavePlanCalculator"/> -- the figures of a leave plan, ported from legacy
/// <c>HCM10.Module/BusinessObjects/Workflow/Leave/LeavePlanner.cs</c>: <c>PlannedDays</c> (lines 38-62),
/// <c>ProjectedLeaveBalance</c> (lines 64-83) and <c>PerformValidations</c> (lines 210-230).
///
/// <para>
/// The accrual and leave-day engines are substituted: what is under test is that the plan asks them the
/// legacy question (annual leave type "1", the salary profile in force on the start date) -- not their own
/// arithmetic, which has its own tests.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeavePlanCalculator_Tests
{
    private const int EmployeeId = 12;
    private const int AnnualLeaveTypeId = 3;

    private sealed class Harness
    {
        public readonly List<AbsenceType> AbsenceTypes = new List<AbsenceType>
        {
            new AbsenceType { Id = 9, Code = "2", NameEn = "Unpaid" },
            new AbsenceType { Id = AnnualLeaveTypeId, Code = "1", NameEn = "Annual Paid Leave" }
        };

        public readonly List<EmployeeOrgAssignment> Assignments = new List<EmployeeOrgAssignment>();
        public readonly IAccrualService Accrual = Substitute.For<IAccrualService>();
        public readonly IEmployeeAbsenceCalculationService Absences = Substitute.For<IEmployeeAbsenceCalculationService>();

        public LeavePlanCalculator Build() =>
            new LeavePlanCalculator(Repo(AbsenceTypes), Repo(Assignments), Accrual, Absences, null);
    }

    // ---------------------------------------------------------------- planned days

    [Theory]
    [InlineData("2026-11-01", "2026-11-01", 1)]
    [InlineData("2026-11-01", "2026-11-10", 10)]
    [InlineData("2026-12-28", "2027-01-03", 7)]
    public void Planned_Days_Should_Be_Calendar_Days_With_Both_Ends_Included(string start, string end, double expected)
    {
        // Legacy line 50: EndDate.Subtract(StartDate).Days + 1 -- calendar days, weekends and holidays in.
        new Harness().Build()
            .GetPlannedDays(DateTime.Parse(start), DateTime.Parse(end))
            .ShouldBe(expected);
    }

    [Fact]
    public void Planned_Days_Should_Be_Zero_While_A_Date_Is_Missing()
    {
        // Legacy line 52 returns 0 when either date is unset; a draft is allowed to be in that state.
        var calculator = new Harness().Build();

        calculator.GetPlannedDays(default, new DateTime(2026, 11, 10)).ShouldBe(0);
        calculator.GetPlannedDays(new DateTime(2026, 11, 1), default).ShouldBe(0);
    }

    [Fact]
    public void Planned_Days_Should_Be_Zero_When_The_End_Is_Before_The_Start()
    {
        // Legacy would have shown a negative number until validation refused the save; nothing is gained by
        // showing it.
        new Harness().Build()
            .GetPlannedDays(new DateTime(2026, 11, 10), new DateTime(2026, 11, 1))
            .ShouldBe(0);
    }

    // ---------------------------------------------------------------- projected balance

    [Fact]
    public async Task Projected_Balance_Should_Ask_The_Accrual_Engine_For_Annual_Leave_On_The_Start_Date()
    {
        // Legacy line 75-77: AbsenceType Code "1", AccrualService.GetAsOnDateQuotaWithProjection(..., StartDate).
        var harness = new Harness();
        var start = new DateTime(2026, 11, 1);
        harness.Accrual
            .GetAsOnDateQuotaWithProjectionAsync(EmployeeId, Arg.Any<int?>(), AnnualLeaveTypeId, start)
            .Returns(Task.FromResult(17.25));

        var projected = await harness.Build().GetProjectedLeaveBalanceAsync(EmployeeId, start);

        projected.ShouldBe(17.25);
    }

    [Fact]
    public async Task Projected_Balance_Should_Use_The_Salary_Profile_In_Force_On_The_Start_Date()
    {
        // Legacy AccrualService.GetAsOnDateQuotaWithProjection reads the EmployeeSubGroup of the org assignment
        // covering the as-on date -- not the current one.
        var harness = new Harness();
        harness.Assignments.Add(new EmployeeOrgAssignment
        {
            Id = 1, EmployeeId = EmployeeId, StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2026, 12, 31), EmployeeSubGroupId = 40
        });
        harness.Assignments.Add(new EmployeeOrgAssignment
        {
            Id = 2, EmployeeId = EmployeeId, StartDate = new DateTime(2027, 1, 1), EndDate = new DateTime(9999, 12, 31), EmployeeSubGroupId = 41
        });

        var start = new DateTime(2027, 3, 1);

        await harness.Build().GetProjectedLeaveBalanceAsync(EmployeeId, start);

        _ = harness.Accrual.Received(1).GetAsOnDateQuotaWithProjectionAsync(EmployeeId, 41, AnnualLeaveTypeId, start);
    }

    [Fact]
    public async Task Projected_Balance_Should_Be_Rounded_To_Three_Places()
    {
        // Legacy BaseRepository.GetProjectedLeaveBalance: Math.Round(..., 3).
        var harness = new Harness();
        harness.Accrual
            .GetAsOnDateQuotaWithProjectionAsync(Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(10.123456));

        (await harness.Build().GetProjectedLeaveBalanceAsync(EmployeeId, new DateTime(2026, 11, 1))).ShouldBe(10.123);
    }

    [Fact]
    public async Task Projected_Balance_Should_Be_Null_Without_An_Annual_Leave_Type()
    {
        // Legacy returned 0, which reads as "you have no leave". Null says "cannot be worked out".
        var harness = new Harness();
        harness.AbsenceTypes.RemoveAll(t => t.Code == "1");

        (await harness.Build().GetProjectedLeaveBalanceAsync(EmployeeId, new DateTime(2026, 11, 1))).ShouldBeNull();

        _ = harness.Accrual.DidNotReceiveWithAnyArgs()
            .GetAsOnDateQuotaWithProjectionAsync(default, default, default, default);
    }

    [Fact]
    public async Task Projected_Balance_Should_Be_Null_Without_A_Start_Date()
    {
        var harness = new Harness();

        (await harness.Build().GetProjectedLeaveBalanceAsync(EmployeeId, default)).ShouldBeNull();
    }

    // ---------------------------------------------------------------- leave days (information only)

    [Fact]
    public async Task Leave_Days_Should_Come_From_The_Leave_Calculation_For_Annual_Leave()
    {
        var harness = new Harness();
        var start = new DateTime(2026, 11, 1);
        var end = new DateTime(2026, 11, 10);
        harness.Absences.CalculateTotalLeaveDaysAsync(EmployeeId, AnnualLeaveTypeId, start, end, 0).Returns(Task.FromResult(8.0));

        (await harness.Build().GetAnnualLeaveDaysAsync(EmployeeId, start, end)).ShouldBe(8);
    }

    [Fact]
    public async Task Leave_Days_Should_Be_Null_When_The_Dates_Are_Incomplete()
    {
        var harness = new Harness();

        (await harness.Build().GetAnnualLeaveDaysAsync(EmployeeId, new DateTime(2026, 11, 1), default)).ShouldBeNull();
    }

    // ---------------------------------------------------------------- validation

    [Fact]
    public void Validate_Should_Refuse_A_Missing_Start_Date()
    {
        Should.Throw<UserFriendlyException>(() => new Harness().Build().Validate(default, new DateTime(2026, 11, 1)));
    }

    [Fact]
    public void Validate_Should_Refuse_A_Missing_End_Date()
    {
        Should.Throw<UserFriendlyException>(() => new Harness().Build().Validate(new DateTime(2026, 11, 1), default));
    }

    [Fact]
    public void Validate_Should_Refuse_An_End_Before_The_Start()
    {
        // Legacy line 223: "EndDate cannot be earlier than Start Date."
        Should.Throw<UserFriendlyException>(() =>
            new Harness().Build().Validate(new DateTime(2026, 11, 10), new DateTime(2026, 11, 9)));
    }

    [Fact]
    public void Validate_Should_Accept_A_One_Day_Plan()
    {
        Should.NotThrow(() => new Harness().Build().Validate(new DateTime(2026, 11, 10), new DateTime(2026, 11, 10)));
    }

    [Fact]
    public async Task Refresh_Should_Write_Both_Figures_Onto_The_Plan()
    {
        var harness = new Harness();
        var start = new DateTime(2026, 11, 1);
        harness.Accrual.GetAsOnDateQuotaWithProjectionAsync(EmployeeId, Arg.Any<int?>(), AnnualLeaveTypeId, start).Returns(Task.FromResult(5.0));

        var plan = new LeavePlan { EmployeeId = EmployeeId, StartDate = start, EndDate = new DateTime(2026, 11, 5) };

        await harness.Build().RefreshFiguresAsync(plan);

        plan.PlannedDays.ShouldBe(5);
        plan.ProjectedLeaveBalance.ShouldBe(5);
    }
}
