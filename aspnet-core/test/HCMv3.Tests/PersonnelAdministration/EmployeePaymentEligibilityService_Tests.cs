using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
/// <see cref="EmployeePaymentEligibilityService"/>, ported from legacy
/// <c>EmployeePayment.GetAllowedEmployeesForPayment</c> (the Payment screen's Employee lookup).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeePaymentEligibilityService_Tests
{
    private const int ActiveId = 1;
    private const int SeparatedId = 2;
    private const int PayrollAreaId = 9;

    private static readonly DateTime Submitted = new DateTime(2025, 3, 10);

    private sealed class Harness
    {
        public readonly List<Employee> Employees = new List<Employee>
        {
            new Employee { Id = ActiveId, Code = "A1", Status = EmployeeStatus.Active },
            new Employee { Id = SeparatedId, Code = "S1", Status = EmployeeStatus.Inactive }
        };
        public readonly List<EmployeeSeparation> Separations = new List<EmployeeSeparation>();
        public readonly List<EmployeeRejoin> Rejoins = new List<EmployeeRejoin>();
        public readonly List<EmployeeFnFPayment> FnFPayments = new List<EmployeeFnFPayment>();
        public readonly List<PayrollControlRecord> ControlRecords = new List<PayrollControlRecord>();
        public readonly IEmployeePayrollAreaResolver Resolver = Substitute.For<IEmployeePayrollAreaResolver>();

        public Harness(bool withPayrollArea = true)
        {
            Resolver.GetEmployeePayrollAreaAsync(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(Task.FromResult(withPayrollArea ? new PayrollArea { Id = PayrollAreaId } : null));
        }

        public void Separation(DateTime? revoked = null) =>
            Separations.Add(new EmployeeSeparation { Id = Separations.Count + 1, EmployeeId = SeparatedId, SubmissionDate = Submitted, RevokeDate = revoked });

        public void FnF(DateTime compensationDate) =>
            FnFPayments.Add(new EmployeeFnFPayment { Id = FnFPayments.Count + 1, EmployeeId = SeparatedId, CompensationDate = compensationDate });

        /// <summary>The March 2025 control record.</summary>
        public void March2025(bool closed) =>
            ControlRecords.Add(new PayrollControlRecord
            {
                Id = ControlRecords.Count + 1,
                PayrollAreaId = PayrollAreaId,
                CalendarMonthStart = new DateTime(2025, 3, 1),
                CalendarMonthEnd = new DateTime(2025, 3, 31),
                PeriodClosed = closed
            });

        public Task<List<int>> Run() =>
            new EmployeePaymentEligibilityService(
                Repo(Employees), Repo(Separations), Repo(Rejoins), Repo(FnFPayments), Repo(ControlRecords), Resolver)
                .GetAllowedEmployeeIdsAsync();
    }

    [Fact]
    public async Task Should_Allow_Active_And_Exclude_Inactive_Without_Separation()
    {
        var result = await new Harness().Run();

        result.ShouldBe(new[] { ActiveId }, ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Allow_Separated_Employee_With_No_Final_Settlement()
    {
        var harness = new Harness();
        harness.Separation();

        (await harness.Run()).ShouldContain(SeparatedId);
    }

    [Fact]
    public async Task Should_Exclude_Revoked_Separation()
    {
        var harness = new Harness();
        harness.Separation(revoked: new DateTime(2025, 3, 15));

        (await harness.Run()).ShouldNotContain(SeparatedId);
    }

    [Fact]
    public async Task Should_Exclude_Employee_Who_Rejoined_After_Separation()
    {
        var harness = new Harness();
        harness.Separation();
        harness.Rejoins.Add(new EmployeeRejoin { Id = 1, EmployeeId = SeparatedId, RejoinDate = new DateTime(2025, 6, 1) });

        (await harness.Run()).ShouldNotContain(SeparatedId);
    }

    [Fact]
    public async Task Should_Ignore_Final_Settlement_Before_Submission_Date()
    {
        var harness = new Harness();
        harness.Separation();
        harness.FnF(new DateTime(2025, 1, 31));

        (await harness.Run()).ShouldContain(SeparatedId);
    }

    [Fact]
    public async Task Should_Allow_Final_Settlement_In_Open_Period()
    {
        var harness = new Harness();
        harness.Separation();
        harness.FnF(new DateTime(2025, 3, 31));
        harness.March2025(closed: false);

        (await harness.Run()).ShouldContain(SeparatedId);
    }

    [Fact]
    public async Task Should_Exclude_Final_Settlement_Outside_Open_Period()
    {
        var harness = new Harness();
        harness.Separation();
        harness.FnF(new DateTime(2025, 4, 30));
        harness.March2025(closed: false);

        (await harness.Run()).ShouldNotContain(SeparatedId);
    }

    [Fact]
    public async Task Should_Use_Following_Month_When_Period_Is_Closed()
    {
        var harness = new Harness();
        harness.Separation();
        harness.FnF(new DateTime(2025, 4, 20));
        harness.March2025(closed: true);

        (await harness.Run()).ShouldContain(SeparatedId);
    }

    [Fact]
    public async Task Should_Exclude_Final_Settlement_In_Closed_Month()
    {
        var harness = new Harness();
        harness.Separation();
        harness.FnF(new DateTime(2025, 3, 31));
        harness.March2025(closed: true);

        (await harness.Run()).ShouldNotContain(SeparatedId);
    }

    [Fact]
    public async Task Should_Exclude_Final_Settlement_When_No_Payroll_Area()
    {
        var harness = new Harness(withPayrollArea: false);
        harness.Separation();
        harness.FnF(new DateTime(2025, 3, 31));
        harness.March2025(closed: false);

        (await harness.Run()).ShouldNotContain(SeparatedId);
    }
}
