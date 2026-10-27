using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.EngineRuns;
using HCMv3.TimeManagement.Dto;
using HCMv3.TimeManagement.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The Time Engine screen follows the run of the payroll area it has selected -- status, per-employee grid and
/// Stop -- and never shows the per-employee runs the payroll engine makes for itself (legacy never did). Also the
/// day-level log behind each employee (legacy's TimeLog grid) and its Excel export, and the run history.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineAppService_RunProgress_Tests : TimeEngineAppServiceTestBase
{
    private static readonly string AreaA = TimeEngineRunKeys.ForPayrollArea("Area A");
    private static readonly string AreaB = TimeEngineRunKeys.ForPayrollArea("Area B");
    private static readonly string PayrollAutorun = TimeEngineRunKeys.FromPayrollEnginePrefix + AreaA;

    // ---------------------------------------------------------------- following the selected area

    [Fact]
    public async Task Should_Follow_The_Latest_Run_Of_The_Selected_Area()
    {
        var runA = SeedEngineRun(AreaA, total: 4);
        SeedEngineRun(AreaB, total: 9);

        var status = await TimeEngineAppService.GetTimeEngineStatus("Area A");

        status.RunId.ShouldBe(runA);
        status.TotalEmployees.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Match_The_Area_Whatever_Its_Casing()
    {
        var runA = SeedEngineRun(AreaA);
        SeedEngineRun(AreaB);

        (await TimeEngineAppService.GetTimeEngineStatus(" area a ")).RunId.ShouldBe(runA);
    }

    [Fact]
    public async Task Should_Follow_The_Latest_Run_Of_Any_Area_When_None_Is_Selected()
    {
        SeedEngineRun(AreaA);
        var runB = SeedEngineRun(AreaB);

        (await TimeEngineAppService.GetTimeEngineStatus()).RunId.ShouldBe(runB);
    }

    [Fact]
    public async Task Should_Say_No_Run_Yet_For_An_Area_That_Has_Never_Run()
    {
        SeedEngineRun(AreaB);

        (await TimeEngineAppService.GetTimeEngineStatus("Area A")).HasRun.ShouldBeFalse();
    }

    /// <summary>One run per employee paid would bury the operator's run; legacy's screen never showed them.</summary>
    [Fact]
    public async Task Should_Never_Show_The_Payroll_Engines_Own_Runs()
    {
        var operatorRun = SeedEngineRun(AreaA);
        SeedEngineRun(PayrollAutorun);
        SeedEngineRun(PayrollAutorun);

        (await TimeEngineAppService.GetTimeEngineStatus("Area A")).RunId.ShouldBe(operatorRun);
        (await TimeEngineAppService.GetTimeEngineStatus()).RunId.ShouldBe(operatorRun);
    }

    [Fact]
    public async Task Should_Show_The_Selected_Areas_Employees()
    {
        var employeeA = CreateEmployee("A1", null);
        var employeeB = CreateEmployee("B1", null);
        var runA = SeedEngineRun(AreaA);
        var runB = SeedEngineRun(AreaB);
        SeedEngineRunEmployee(runA, 1, employeeA, "A1", true, "Successfull");
        SeedEngineRunEmployee(runB, 1, employeeB, "B1", true, "Successfull");

        var rows = await TimeEngineAppService.GetTimeEngineRunEmployees(new GetTimeEngineRunEmployeesInput
        {
            PayrollAreaName = "Area A",
            MaxResultCount = 10
        });

        rows.Items.Select(r => r.EmployeeCode).ShouldBe(new[] { "A1" });
    }

    /// <summary>Stopping one area's run leaves another area's alone.</summary>
    [Fact]
    public async Task Should_Stop_Only_The_Selected_Areas_Run()
    {
        var runA = SeedEngineRun(AreaA, EngineRunStatus.Running, startedAt: Clock.Now);
        var runB = SeedEngineRun(AreaB, EngineRunStatus.Running, startedAt: Clock.Now);

        await TimeEngineAppService.StopTimeEngine("Area A");

        await UsingDbContextAsync(async context =>
        {
            (await context.EngineRuns.FirstAsync(r => r.Id == runA)).StopRequested.ShouldBeTrue();
            (await context.EngineRuns.FirstAsync(r => r.Id == runB)).StopRequested.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Should_Never_Stop_A_Payroll_Runs_Own_Time_Engine_Call()
    {
        var autorun = SeedEngineRun(PayrollAutorun, EngineRunStatus.Running, startedAt: Clock.Now);

        await TimeEngineAppService.StopTimeEngine();

        await UsingDbContextAsync(async context =>
            (await context.EngineRuns.FirstAsync(r => r.Id == autorun)).StopRequested.ShouldBeFalse());
    }

    // ---------------------------------------------------------------- the day-level log

    [Fact]
    public async Task Should_List_One_Employees_Days_With_Their_Status_And_Message()
    {
        var employee = CreateEmployee("D1", null);
        var started = Clock.Now.AddMinutes(-10);
        var run = SeedEngineRun(AreaA, startedAt: started, completedAt: Clock.Now.AddMinutes(1));
        SeedEngineRunEmployee(run, 1, employee, "D1", false, "1 day failed");
        SeedTimeLog(employee, new DateTime(2026, 9, 2), 1, "Employee Duty Period not found", started.AddMinutes(1));
        SeedTimeLog(employee, new DateTime(2026, 9, 1), 0, "Successfull", started.AddMinutes(1));

        var days = await TimeEngineAppService.GetTimeEngineRunDays(new GetTimeEngineRunDaysInput
        {
            RunId = run,
            EmployeeId = employee
        });

        days.Select(d => d.DayDate).ShouldBe(new[] { new DateTime(2026, 9, 1), new DateTime(2026, 9, 2) });
        days[0].Succeeded.ShouldBeTrue();
        days[1].Succeeded.ShouldBeFalse();
        days[1].Message.ShouldBe("Employee Duty Period not found");
        days[1].EmployeeCode.ShouldBe("D1");
    }

    [Fact]
    public async Task Should_List_Only_The_Failed_Days_When_Asked()
    {
        var employee = CreateEmployee("D2", null);
        var started = Clock.Now.AddMinutes(-10);
        var run = SeedEngineRun(AreaA, startedAt: started, completedAt: Clock.Now.AddMinutes(1));
        SeedEngineRunEmployee(run, 1, employee, "D2", false, "1 day failed");
        SeedTimeLog(employee, new DateTime(2026, 9, 1), 0, "Successfull", started.AddMinutes(1));
        SeedTimeLog(employee, new DateTime(2026, 9, 2), 1, "failed", started.AddMinutes(1));

        var days = await TimeEngineAppService.GetTimeEngineRunDays(new GetTimeEngineRunDaysInput
        {
            RunId = run,
            EmployeeId = employee,
            OnlyFailed = true
        });

        days.ShouldHaveSingleItem().DayDate.ShouldBe(new DateTime(2026, 9, 2));
    }

    /// <summary>TimeLog has no run id: a run's rows are its employees' rows stamped while it was running.</summary>
    [Fact]
    public async Task Should_Leave_Out_Rows_Written_Before_Or_After_The_Run()
    {
        var employee = CreateEmployee("D3", null);
        var started = Clock.Now.AddMinutes(-10);
        var ended = Clock.Now.AddMinutes(-5);
        var run = SeedEngineRun(AreaA, startedAt: started, completedAt: ended);
        SeedEngineRunEmployee(run, 1, employee, "D3", true, "Successfull");
        SeedTimeLog(employee, new DateTime(2026, 8, 31), 1, "an earlier run", started.AddMinutes(-1));
        SeedTimeLog(employee, new DateTime(2026, 9, 1), 0, "Successfull", started.AddMinutes(1));
        SeedTimeLog(employee, new DateTime(2026, 9, 2), 1, "a later run", ended.AddMinutes(1));

        var days = await TimeEngineAppService.GetTimeEngineRunDays(new GetTimeEngineRunDaysInput
        {
            RunId = run,
            EmployeeId = employee
        });

        days.ShouldHaveSingleItem().DayDate.ShouldBe(new DateTime(2026, 9, 1));
    }

    [Fact]
    public async Task Should_Return_No_Days_For_Another_Engines_Run()
    {
        var employee = CreateEmployee("D4", null);
        var started = Clock.Now.AddMinutes(-10);
        var payrollRun = SeedEngineRun("PayrollArea:AREA A", startedAt: started, engineType: EngineType.PayrollEngine);
        SeedEngineRunEmployee(payrollRun, 1, employee, "D4", true, "Successfull");
        SeedTimeLog(employee, new DateTime(2026, 9, 1), 0, "Successfull", started.AddMinutes(1));

        var days = await TimeEngineAppService.GetTimeEngineRunDays(new GetTimeEngineRunDaysInput
        {
            RunId = payrollRun,
            EmployeeId = employee
        });

        days.ShouldBeEmpty();
    }

    /// <summary>Legacy btnExportTimeLog_Click: the log, one row per employee per day, as TimeEngineExecutionLog_*.xlsx.</summary>
    [Fact]
    public async Task Should_Export_The_Selected_Areas_Latest_Run_Log_To_Excel()
    {
        var employee = CreateEmployee("X1", null);
        var started = Clock.Now.AddMinutes(-10);
        var run = SeedEngineRun(AreaA, startedAt: started, completedAt: Clock.Now.AddMinutes(1));
        SeedEngineRunEmployee(run, 1, employee, "X1", true, "Successfull");
        SeedTimeLog(employee, new DateTime(2026, 9, 1), 0, "Successfull", started.AddMinutes(1));

        var file = await TimeEngineAppService.GetTimeEngineRunLogToExcel(new GetTimeEngineRunLogToExcelInput
        {
            PayrollAreaName = "Area A"
        });

        file.ShouldNotBeNull();
        file.FileName.ShouldStartWith("TimeEngineExecutionLog_");
        file.FileName.ShouldEndWith(".xlsx");
        file.FileToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Export_An_Empty_File_When_The_Area_Has_Never_Run()
    {
        var file = await TimeEngineAppService.GetTimeEngineRunLogToExcel(new GetTimeEngineRunLogToExcelInput
        {
            PayrollAreaName = "Never Ran"
        });

        file.FileName.ShouldStartWith("TimeEngineExecutionLog_");
    }

    // ---------------------------------------------------------------- run history

    [Fact]
    public async Task Should_Leave_The_Payroll_Engines_Runs_Out_Of_The_History()
    {
        SeedExecutionLog("admin", Clock.Now.AddMinutes(-3), "Payroll Area : Area A");
        SeedExecutionLog(TimeEngineRunKeys.PayrollEngineOriginatorUserName, Clock.Now.AddMinutes(-2), "Employee Code : E1");
        SeedExecutionLog(TimeEngineRunKeys.PayrollEngineOriginatorUserName, Clock.Now.AddMinutes(-1), "Employee Code : E2");

        var history = await TimeEngineAppService.GetTimeEngineRunHistory(new GetTimeEngineRunHistoryInput { MaxResultCount = 10 });

        history.TotalCount.ShouldBe(1);
        history.Items.ShouldHaveSingleItem().UserName.ShouldBe("admin");
    }

    [Fact]
    public async Task Should_List_The_History_Newest_First()
    {
        SeedExecutionLog("first", Clock.Now.AddMinutes(-3), "Payroll Area : Area A");
        SeedExecutionLog("second", Clock.Now.AddMinutes(-2), "Payroll Area : Area A");
        SeedExecutionLog("SysJobAdmin", Clock.Now.AddMinutes(-1), "Payroll Area : Area A");

        var history = await TimeEngineAppService.GetTimeEngineRunHistory(new GetTimeEngineRunHistoryInput { MaxResultCount = 10 });

        history.Items.Select(i => i.UserName).ShouldBe(new[] { "SysJobAdmin", "second", "first" });
    }
}
