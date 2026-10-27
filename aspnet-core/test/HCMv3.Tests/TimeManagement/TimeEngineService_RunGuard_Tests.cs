using System;
using System.Reflection;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.EngineRuns;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement.Services;
using HCMv3.Tests.EngineRuns;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// ONE TIME ENGINE RUN PER PAYROLL AREA, and keeping the payroll engine's own per-employee runs off the Time Engine
/// screen. Neither is a port -- legacy had no guard and showed only its truncated TimeLog -- see
/// <see cref="TimeEngineRunKeys"/>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineService_RunGuard_Tests
{
    private readonly TimeEngineServiceTestHarness _harness = new TimeEngineServiceTestHarness();
    private readonly RecordingEngineRunProgressWriter _progress = new RecordingEngineRunProgressWriter();

    public TimeEngineService_RunGuard_Tests()
    {
        _harness.Service.ProgressWriter = _progress;
        _harness.Add(new Employee { Code = "A1", FullNameEn = "Alice", Status = EmployeeStatus.Active }, 1);
    }

    [Fact]
    public async Task Should_File_An_Area_Run_Under_The_Areas_Key()
    {
        await _harness.Service.CreateTimeRegisterAsync(NewInput("Area A"));

        _progress.StartedRuns.ShouldHaveSingleItem().RunKey.ShouldBe(TimeEngineRunKeys.ForPayrollArea("Area A"));
    }

    [Fact]
    public async Task Should_Refuse_A_Second_Run_Of_An_Area_That_Is_Running()
    {
        _progress.ActiveRunKey = TimeEngineRunKeys.ForPayrollArea("Area A");

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _harness.Service.CreateTimeRegisterAsync(NewInput("Area A")));

        exception.Message.ShouldContain("Area A");
        exception.Message.ShouldContain(_progress.StartedByUserName);
        _progress.StartedRuns.ShouldBeEmpty("a refused run must not open a progress row that hides the running one");
    }

    [Fact]
    public async Task Should_Let_Another_Area_Run_At_The_Same_Time()
    {
        _progress.ActiveRunKey = TimeEngineRunKeys.ForPayrollArea("Area B");

        await Should.NotThrowAsync(() => _harness.Service.CreateTimeRegisterAsync(NewInput("Area A")));

        _progress.Completed.ShouldBeTrue();
    }

    [Fact]
    public void Should_Treat_Casing_And_Spaces_As_The_Same_Area()
    {
        TimeEngineRunKeys.ForPayrollArea(" area a ").ShouldBe(TimeEngineRunKeys.ForPayrollArea("AREA A"));
        TimeEngineRunKeys.ForPayrollArea("Area A").ShouldNotBe(TimeEngineRunKeys.ForPayrollArea("Area B"));
        TimeEngineRunKeys.ForPayrollArea(" ").ShouldBeNull();
    }

    /// <summary>A run of named employees only is filed under the first employee's area, as the screen's checks resolve it.</summary>
    [Fact]
    public async Task Should_File_A_Codes_Only_Run_Under_The_First_Employees_Area()
    {
        _harness.PayrollAreaResolver.GetEmployeePayrollAreaAsync(1, Arg.Any<DateTime>())
            .Returns(Task.FromResult(new PayrollArea { Name = "Area A" }));

        await _harness.Service.CreateTimeRegisterAsync(NewInput(null, employeeCodes: "A1,ZZ"));

        _progress.StartedRuns.ShouldHaveSingleItem().RunKey.ShouldBe(TimeEngineRunKeys.ForPayrollArea("Area A"));
    }

    [Fact]
    public async Task Should_Refuse_A_Codes_Only_Run_While_The_First_Employees_Area_Is_Running()
    {
        _harness.PayrollAreaResolver.GetEmployeePayrollAreaAsync(1, Arg.Any<DateTime>())
            .Returns(Task.FromResult(new PayrollArea { Name = "Area A" }));
        _progress.ActiveRunKey = TimeEngineRunKeys.ForPayrollArea("Area A");

        await Should.ThrowAsync<UserFriendlyException>(
            () => _harness.Service.CreateTimeRegisterAsync(NewInput(null, employeeCodes: "A1")));
    }

    /// <summary>
    /// The payroll engine's per-employee autorun runs inside a payroll run and must never fail because someone
    /// pressed Run on the Time Engine screen -- and must not show up on that screen.
    /// </summary>
    [Fact]
    public async Task Should_Neither_Guard_Nor_Show_The_Payroll_Engines_Autorun()
    {
        _progress.ActiveRunKey = TimeEngineRunKeys.ForPayrollArea("Area A");

        var input = NewInput("Area A", employeeCodes: "A1");
        input.IsRanFromPayrollEngine = true;
        input.CurrentUserName = TimeEngineRunKeys.PayrollEngineOriginatorUserName;

        await Should.NotThrowAsync(() => _harness.Service.CreateTimeRegisterAsync(input));

        var runKey = _progress.StartedRuns.ShouldHaveSingleItem().RunKey;
        runKey.ShouldStartWith(TimeEngineRunKeys.FromPayrollEnginePrefix);
        TimeEngineRunKeys.IsHiddenFromTimeEngineScreen(runKey).ShouldBeTrue();
    }

    /// <summary>
    /// Go-Live Setup also runs the time engine "as the payroll engine would", but under the operator's name and in
    /// its own batches: unguarded as before, and still filed under the area, so the screen keeps showing it.
    /// </summary>
    [Fact]
    public async Task Should_Keep_Go_Live_Runs_Unguarded_And_On_The_Screen()
    {
        _progress.ActiveRunKey = TimeEngineRunKeys.ForPayrollArea("Area A");

        var input = NewInput("Area A", employeeCodes: "A1");
        input.IsRanFromPayrollEngine = true;

        await Should.NotThrowAsync(() => _harness.Service.CreateTimeRegisterAsync(input));

        _progress.StartedRuns.ShouldHaveSingleItem().RunKey.ShouldBe(TimeEngineRunKeys.ForPayrollArea("Area A"));
    }

    /// <summary>
    /// The screen recognises the payroll engine's calls by the user name that engine passes. That constant is private
    /// to <see cref="PayrollEngineService"/>, so the two are pinned together here: renaming one without the other
    /// would flood the Time Engine screen with one-employee runs.
    /// </summary>
    [Fact]
    public void Should_Match_The_User_Name_The_Payroll_Engine_Passes()
    {
        var field = typeof(PayrollEngineService).GetField(
            "PayrollEngineOriginatorUserName", BindingFlags.NonPublic | BindingFlags.Static);

        field.ShouldNotBeNull();
        field.GetValue(null).ShouldBe(TimeEngineRunKeys.PayrollEngineOriginatorUserName);
    }

    private static TimeEngineRunInput NewInput(string payrollAreaName, string employeeCodes = null)
    {
        return new TimeEngineRunInput
        {
            PayrollAreaName = payrollAreaName,
            EmployeeCodes = employeeCodes,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            CurrentUserName = "operator"
        };
    }
}
