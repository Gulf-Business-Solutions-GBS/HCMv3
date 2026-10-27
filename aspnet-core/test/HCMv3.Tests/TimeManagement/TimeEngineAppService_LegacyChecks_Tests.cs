using System;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.Timing;
using Abp.UI;
using HCMv3.EngineRuns;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The checks legacy's Time Engine screen made before it queued a run (<c>uscTimeEngine.ascx.cs</c> lines
/// 173-290), in its order, plus the one-run-per-area guard legacy never had:
/// <list type="bullet">
/// <item><description>a selected payroll area needs its control record -- "Payroll Control Record not found.";</description></item>
/// <item><description>with no area, the FIRST employee code's area (its org assignment covering today) must exist, and so
/// must that area's control record;</description></item>
/// <item><description>with neither -- "Cannot have Payroll Area unselected if the Employee Code is blank."</description></item>
/// </list>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineAppService_LegacyChecks_Tests : TimeEngineAppServiceTestBase
{
    private static readonly DateTime MonthEnd = new DateTime(2024, 6, 30);

    public TimeEngineAppService_LegacyChecks_Tests()
    {
        SetTimeSettings(futureEvaluationInDays: 3650, earliestEvaluationDate: new DateTime(2020, 1, 1));
    }

    [Fact]
    public async Task Should_Refuse_A_Run_With_Neither_A_Payroll_Area_Nor_Employee_Codes()
    {
        await ShouldBeRefusedWith("TimeEnginePayrollAreaOrEmployeeCodeRequired", new RunTimeEngineInput
        {
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });
    }

    [Fact]
    public async Task Should_Treat_Blank_Area_And_Codes_As_Neither()
    {
        await ShouldBeRefusedWith("TimeEnginePayrollAreaOrEmployeeCodeRequired", new RunTimeEngineInput
        {
            PayrollAreaName = "  ",
            EmployeeCodes = " ",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });
    }

    [Fact]
    public async Task Should_Refuse_A_Payroll_Area_That_Has_No_Control_Record()
    {
        CreatePayrollArea("No Record Area", calendarMonthEnd: null);

        await ShouldBeRefusedWith("TimeEnginePayrollControlRecordNotFound", new RunTimeEngineInput
        {
            PayrollAreaName = "No Record Area",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });
    }

    [Fact]
    public async Task Should_Refuse_An_Unknown_Payroll_Area_As_Having_No_Control_Record()
    {
        await ShouldBeRefusedWith("TimeEnginePayrollControlRecordNotFound", new RunTimeEngineInput
        {
            PayrollAreaName = "No such area",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2030, 12, 31)
        });
    }

    /// <summary>Legacy checks the area branch first: with an area selected, the codes play no part in the checks.</summary>
    [Fact]
    public async Task Should_Check_The_Selected_Area_Even_When_Codes_Are_Typed()
    {
        var area = CreatePayrollArea("Area With Record", MonthEnd);
        CreateEmployee("OTHER1", payrollAreaId: null);

        var output = await TimeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            PayrollAreaName = area.Name,
            EmployeeCodes = "OTHER1",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });

        output.Queued.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Queue_A_Codes_Only_Run_Through_The_First_Employees_Area()
    {
        var area = CreatePayrollArea("Codes Area", MonthEnd);
        CreateEmployee("C1", area.Id);

        var output = await TimeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            EmployeeCodes = "C1",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });

        output.Queued.ShouldBeTrue();

        // The job is handed what was typed: no area is invented for the engine (its employee selection is by code).
        EnqueuedArgs.PayrollAreaName.ShouldBeNull();
        EnqueuedArgs.EmployeeCodes.ShouldBe("C1");
    }

    /// <summary>Only the FIRST code decides: a second employee without an area does not refuse the run.</summary>
    [Fact]
    public async Task Should_Resolve_The_Area_From_The_First_Code_Only()
    {
        var area = CreatePayrollArea("First Code Area", MonthEnd);
        CreateEmployee("F1", area.Id);
        CreateEmployee("F2", payrollAreaId: null);

        var output = await TimeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            EmployeeCodes = " F1 , F2",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });

        output.Queued.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_A_Codes_Only_Run_Whose_First_Employee_Has_No_Payroll_Area()
    {
        CreateEmployee("NOAREA", payrollAreaId: null);

        await ShouldBeRefusedWith("TimeEngineEmployeePayrollAreaNotFound", new RunTimeEngineInput
        {
            EmployeeCodes = "NOAREA",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });
    }

    [Fact]
    public async Task Should_Refuse_A_Codes_Only_Run_For_An_Unknown_Employee()
    {
        await ShouldBeRefusedWith("TimeEngineEmployeePayrollAreaNotFound", new RunTimeEngineInput
        {
            EmployeeCodes = "NOBODY",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });
    }

    /// <summary>Legacy looked at the org assignment covering TODAY, not the run window.</summary>
    [Fact]
    public async Task Should_Use_The_Assignment_Covering_Today()
    {
        var area = CreatePayrollArea("Ended Area", MonthEnd);
        CreateEmployee("ENDED", area.Id, assignedFrom: new DateTime(2019, 1, 1), assignedTo: Clock.Now.Date.AddDays(-1));

        await ShouldBeRefusedWith("TimeEngineEmployeePayrollAreaNotFound", new RunTimeEngineInput
        {
            EmployeeCodes = "ENDED",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });
    }

    [Fact]
    public async Task Should_Refuse_A_Codes_Only_Run_Whose_Employees_Area_Has_No_Control_Record()
    {
        var area = CreatePayrollArea("Area Without Record", calendarMonthEnd: null);
        CreateEmployee("NOREC", area.Id);

        await ShouldBeRefusedWith("TimeEngineEmployeeControlRecordNotFound", new RunTimeEngineInput
        {
            EmployeeCodes = "NOREC",
            StartDate = new DateTime(2024, 6, 1),
            EndDate = MonthEnd
        });
    }

    // ---------------------------------------------------------------- one run per payroll area

    [Fact]
    public async Task Should_Refuse_A_Second_Run_Of_An_Area_That_Is_Running()
    {
        var area = CreatePayrollArea("Busy Area", MonthEnd);
        SeedEngineRun(TimeEngineRunKeys.ForPayrollArea("Busy Area"), EngineRunStatus.Running, startedAt: Clock.Now);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput { PayrollAreaName = area.Name, StartDate = new DateTime(2024, 6, 1), EndDate = MonthEnd }));

        exception.Message.ShouldNotBeNullOrWhiteSpace();
        EnqueuedArgs.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Let_Another_Area_Run_While_One_Is_Running()
    {
        CreatePayrollArea("Busy Area", MonthEnd);
        var free = CreatePayrollArea("Free Area", MonthEnd);
        SeedEngineRun(TimeEngineRunKeys.ForPayrollArea("Busy Area"), EngineRunStatus.Running, startedAt: Clock.Now);

        var output = await TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput { PayrollAreaName = free.Name, StartDate = new DateTime(2024, 6, 1), EndDate = MonthEnd });

        output.Queued.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_A_Codes_Only_Run_While_The_Employees_Area_Is_Running()
    {
        var area = CreatePayrollArea("Busy Codes Area", MonthEnd);
        CreateEmployee("BUSY1", area.Id);
        SeedEngineRun(TimeEngineRunKeys.ForPayrollArea("Busy Codes Area"), EngineRunStatus.Running, startedAt: Clock.Now);

        await Should.ThrowAsync<UserFriendlyException>(() => TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput { EmployeeCodes = "BUSY1", StartDate = new DateTime(2024, 6, 1), EndDate = MonthEnd }));
    }

    /// <summary>The payroll engine's own per-employee runs never block the operator.</summary>
    [Fact]
    public async Task Should_Not_Be_Blocked_By_The_Payroll_Engines_Autorun()
    {
        var area = CreatePayrollArea("Payroll Running Area", MonthEnd);
        SeedEngineRun(
            TimeEngineRunKeys.FromPayrollEnginePrefix + TimeEngineRunKeys.ForPayrollArea("Payroll Running Area"),
            EngineRunStatus.Running,
            startedAt: Clock.Now);

        var output = await TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput { PayrollAreaName = area.Name, StartDate = new DateTime(2024, 6, 1), EndDate = MonthEnd });

        output.Queued.ShouldBeTrue();
    }

    private async Task ShouldBeRefusedWith(string localizationKey, RunTimeEngineInput input)
    {
        var exception = await Should.ThrowAsync<UserFriendlyException>(() => TimeEngineAppService.RunTimeEngine(input));

        exception.Message.ShouldBe(Resolve<ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName, localizationKey));
        EnqueuedArgs.ShouldBeNull();
    }
}
