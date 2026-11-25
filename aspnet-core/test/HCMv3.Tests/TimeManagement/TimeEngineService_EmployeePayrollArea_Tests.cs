using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HCMv3.EngineRuns;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using HCMv3.Tests.EngineRuns;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// AN EMPLOYEE WITH NO PAYROLL AREA IS REPORTED IN THE RESULTS. Legacy's screen checked only the FIRST employee
/// code's area and ran everyone else silently. The engine now fails each such employee with
/// "Employee payroll area not found on {date}.", and takes the cutoff day from the employee's OWN area instead of
/// from whichever payroll control record happened to cover the date.
///
/// <para>The fixture uses a VariableTime policy so every day fails cheaply (as in
/// <see cref="TimeEngineService_DayFailures_Tests"/>); what matters here is the employee-level outcome.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineService_EmployeePayrollArea_Tests
{
    private static readonly DateTime RunStart = new DateTime(2026, 1, 1);
    private static readonly DateTime RunEnd = new DateTime(2026, 1, 3);

    private readonly TimeEngineServiceTestHarness _harness = new TimeEngineServiceTestHarness();
    private readonly RecordingEngineRunProgressWriter _progress = new RecordingEngineRunProgressWriter();

    public TimeEngineService_EmployeePayrollArea_Tests()
    {
        _harness.Service.ProgressWriter = _progress;

        _harness.Add(new TimePolicy { TimeMgmtStatus = TimeMgmtStatus.VariableTime }, 1);

        AddEmployee(1, "E1", new DateTime(9999, 12, 31));
        AddEmployee(2, "E2", new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Fail_An_Employee_Without_A_Payroll_Area()
    {
        await RunAsync("E1");

        var row = _progress.Employees.ShouldHaveSingleItem();
        row.EmployeeCode.ShouldBe("E1");
        row.Succeeded.ShouldBeFalse();
        row.Message.ShouldBe("Employee payroll area not found on 03/01/2026.");
    }

    [Fact]
    public async Task Should_Not_Calculate_Any_Day_For_An_Employee_Without_A_Payroll_Area()
    {
        await RunAsync("E1");

        // Only the employee-level failure row, logged against the run start -- no per-day rows.
        var log = _harness.Store<TimeLog>().Where(l => l.EmployeeId == 1).ShouldHaveSingleItem();
        log.Status.ShouldBe(1);
        log.DayDate.ShouldBe(RunStart);
    }

    [Fact]
    public async Task Should_Close_The_Run_With_Errors_When_An_Employee_Has_No_Payroll_Area()
    {
        await RunAsync("E1");

        _progress.CompletedStatus.ShouldBe(EngineRunStatus.CompletedWithErrors);
    }

    /// <summary>Legacy checked only the first code; a later code without an area must still be reported.</summary>
    [Fact]
    public async Task Should_Report_A_Later_Employee_Code_Without_A_Payroll_Area()
    {
        GivenEmployeeInArea(1, new PayrollArea { Name = "Area A" });

        await RunAsync("E1,E2");

        var e2 = _progress.Employees.Single(e => e.EmployeeCode == "E2");
        e2.Succeeded.ShouldBeFalse();
        e2.Message.ShouldBe("Employee payroll area not found on 03/01/2026.");
        _progress.Employees.Single(e => e.EmployeeCode == "E1").Message
            .ShouldNotContain("payroll area not found");
    }

    [Fact]
    public async Task Should_Not_Report_A_Missing_Area_For_An_Employee_That_Has_One()
    {
        GivenEmployeeInArea(1, new PayrollArea { Name = "Area A" });

        await RunAsync("E1");

        // Every day fails on the VariableTime policy, which proves the employee got past the area check.
        _progress.Employees.ShouldHaveSingleItem().Message.ShouldContain("Employee has VariableTime Policy found on ");
    }

    /// <summary>A leaver is judged on the assignment of their last day in service, not the run end date.</summary>
    [Fact]
    public async Task Should_Look_Up_The_Area_On_The_Employees_Last_Day_In_The_Window()
    {
        _harness.Store<EmployeeContract>().Single(c => c.EmployeeId == 1).EndDate = new DateTime(2026, 1, 2);

        await RunAsync("E1");

        await _harness.PayrollAreaResolver.Received().GetEmployeePayrollAreaAsync(1, new DateTime(2026, 1, 2));
        _progress.Employees.Single().Message.ShouldBe("Employee payroll area not found on 02/01/2026.");
    }

    /// <summary>
    /// The cutoff comes from the employee's own area (its CutOffDay and its control record), not from another
    /// area whose control record happens to cover the run end date.
    /// </summary>
    [Fact]
    public async Task Should_Take_The_Cutoff_From_The_Employees_Own_Payroll_Area()
    {
        var otherArea = _harness.Add(new PayrollArea { Name = "Other", CutOffDay = 10 }, 2);
        var ownArea = _harness.Add(new PayrollArea { Name = "Own", CutOffDay = 20 }, 1);

        // Listed first and covering the run end date: what the old lookup would have picked.
        _harness.Add(new PayrollControlRecord
        {
            PayrollAreaId = otherArea.Id,
            CalendarMonthStart = new DateTime(2026, 1, 1),
            CalendarMonthEnd = new DateTime(2026, 1, 31)
        }, 1);
        _harness.Add(new PayrollControlRecord
        {
            PayrollAreaId = ownArea.Id,
            CalendarMonthStart = new DateTime(2026, 2, 1),
            CalendarMonthEnd = new DateTime(2026, 2, 28)
        }, 2);

        GivenEmployeeInArea(1, ownArea);

        var cutoffDate = await ProcessEmployeeAndReadCutoffAsync(_harness.Store<Employee>().Single(e => e.Id == 1));

        cutoffDate.ShouldBe(new DateTime(2026, 2, 20));
    }

    private void AddEmployee(int id, string code, DateTime contractEnd)
    {
        _harness.Add(new Employee
        {
            Code = code,
            FullNameEn = "Employee " + code,
            Status = EmployeeStatus.Active,
            JoiningDate = new DateTime(2020, 1, 15)
        }, id);

        _harness.Add(new EmployeeContract
        {
            EmployeeId = id,
            StartDate = new DateTime(2020, 1, 15),
            EndDate = contractEnd
        }, id);

        _harness.Add(new EmployeeTimePolicy
        {
            EmployeeId = id,
            TimePolicyId = 1,
            StartDate = new DateTime(2020, 1, 15),
            EndDate = new DateTime(9999, 12, 31)
        }, id);
    }

    private void GivenEmployeeInArea(int employeeId, PayrollArea area)
    {
        _harness.PayrollAreaResolver.GetEmployeePayrollAreaAsync(employeeId, Arg.Any<DateTime>())
            .Returns(Task.FromResult(area));
    }

    private Task<TimeEngineRunResult> RunAsync(string employeeCodes)
    {
        return _harness.Service.CreateTimeRegisterAsync(new TimeEngineRunInput
        {
            EmployeeCodes = employeeCodes,
            StartDate = RunStart,
            EndDate = RunEnd,
            CurrentUserName = "operator"
        });
    }

    /// <summary>
    /// The cutoff date lives only on the engine's private run cursor, so the employee step is driven directly with
    /// the engine's own settings and master data, and the cursor read back afterwards.
    /// </summary>
    private async Task<DateTime> ProcessEmployeeAndReadCutoffAsync(Employee employee)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var serviceType = typeof(TimeEngineService);

        var settings = await (Task<TimeEngineSettings>)serviceType.GetMethod("LoadSettingsAsync", flags)
            .Invoke(_harness.Service, null);
        var masterData = (TimeEngineMasterData)serviceType.GetMethod("LoadMasterData", flags)
            .Invoke(_harness.Service, new object[] { settings });

        var cursorType = serviceType.GetNestedType("RunCursor", BindingFlags.NonPublic);
        var cursor = Activator.CreateInstance(cursorType, true);
        cursorType.GetField("CutoffDate").SetValue(cursor, new DateTime(2000, 1, 1));

        var input = new TimeEngineRunInput
        {
            EmployeeCodes = employee.Code,
            StartDate = RunStart,
            EndDate = RunEnd,
            CurrentUserName = "operator"
        };

        await _harness.InvokeAsync("ProcessEmployeeAsync",
            input, employee, RunStart, RunEnd, settings, masterData, cursor, 1);

        return (DateTime)cursorType.GetField("CutoffDate").GetValue(cursor);
    }
}
