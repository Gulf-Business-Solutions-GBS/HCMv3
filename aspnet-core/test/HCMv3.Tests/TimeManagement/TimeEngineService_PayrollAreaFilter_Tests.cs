using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement.Services;
using HCMv3.Tests.EngineRuns;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// BUG: the Time Engine ran every active employee whatever payroll area the operator picked -- the area was looked
/// up and never applied. Legacy (<c>HCM10.TimeEngine/Program.cs</c> GetEmployeeList, lines 2548-2553) runs only the
/// active employees whose org assignment covering the END DATE is in that area. The area is now matched through
/// <c>IEmployeePayrollAreaResolver</c>, as the payroll engine does.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineService_PayrollAreaFilter_Tests
{
    private static readonly DateTime RunEnd = new DateTime(2026, 1, 31);

    private readonly TimeEngineServiceTestHarness _harness = new TimeEngineServiceTestHarness();
    private readonly RecordingEngineRunProgressWriter _progress = new RecordingEngineRunProgressWriter();

    public TimeEngineService_PayrollAreaFilter_Tests()
    {
        _harness.Service.ProgressWriter = _progress;

        _harness.Add(new Employee { Code = "A1", FullNameEn = "In Area A", Status = EmployeeStatus.Active }, 1);
        _harness.Add(new Employee { Code = "B1", FullNameEn = "In Area B", Status = EmployeeStatus.Active }, 2);
        _harness.Add(new Employee { Code = "A2", FullNameEn = "Left Area A", Status = EmployeeStatus.Inactive }, 3);

        GivenAreaHasEmployees("Area A", 1, 3);
        GivenAreaHasEmployees("Area B", 2);
    }

    [Fact]
    public async Task Should_Run_Only_The_Employees_Of_The_Selected_Payroll_Area()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(payrollAreaName: "Area A"));

        _progress.Employees.Select(e => e.EmployeeCode).ShouldBe(new[] { "A1" });
        _progress.TotalsPublished.ShouldHaveSingleItem().ShouldBe(1);
    }

    [Fact]
    public async Task Should_Run_The_Other_Area_Only_When_That_Area_Is_Selected()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(payrollAreaName: "Area B"));

        _progress.Employees.Select(e => e.EmployeeCode).ShouldBe(new[] { "B1" });
    }

    /// <summary>Legacy matched the assignment covering the run's END date, not its start.</summary>
    [Fact]
    public async Task Should_Ask_For_The_Area_As_At_The_Run_End_Date()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(payrollAreaName: "Area A"));

        await _harness.PayrollAreaResolver.Received(1).GetEmployeeIdsInPayrollAreaAsync("Area A", RunEnd);
    }

    /// <summary>Legacy's area branch keeps active employees only (rd.Employee.Status == Active).</summary>
    [Fact]
    public async Task Should_Leave_Out_The_Areas_Inactive_Employees()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(payrollAreaName: "Area A"));

        _progress.Employees.ShouldNotContain(e => e.EmployeeCode == "A2");
    }

    [Fact]
    public async Task Should_Run_Nobody_For_An_Area_With_No_Employees()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(payrollAreaName: "Empty Area"));

        _progress.Employees.ShouldBeEmpty();
        _progress.TotalsPublished.ShouldHaveSingleItem().ShouldBe(0);
    }

    /// <summary>Legacy: the employee-code list wins over the payroll area (GetEmployeeList's first branch).</summary>
    [Fact]
    public async Task Should_Let_Employee_Codes_Win_Over_The_Payroll_Area()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput(payrollAreaName: "Area A", employeeCodes: "B1"));

        _progress.Employees.Select(e => e.EmployeeCode).ShouldBe(new[] { "B1" });
        await _harness.PayrollAreaResolver.DidNotReceive()
            .GetEmployeeIdsInPayrollAreaAsync(Arg.Any<string>(), Arg.Any<DateTime>());
    }

    /// <summary>With neither filter, every active employee -- unchanged.</summary>
    [Fact]
    public async Task Should_Run_Every_Active_Employee_With_Neither_Filter()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput());

        _progress.Employees.Select(e => e.EmployeeCode).ShouldBe(new[] { "A1", "B1" });
    }

    private void GivenAreaHasEmployees(string payrollAreaName, params int[] employeeIds)
    {
        _harness.PayrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName, Arg.Any<DateTime>())
            .Returns(Task.FromResult(employeeIds.ToList()));
    }

    private static TimeEngineRunInput NewInput(string payrollAreaName = null, string employeeCodes = null)
    {
        return new TimeEngineRunInput
        {
            PayrollAreaName = payrollAreaName,
            EmployeeCodes = employeeCodes,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = RunEnd,
            CurrentUserName = "operator"
        };
    }
}
