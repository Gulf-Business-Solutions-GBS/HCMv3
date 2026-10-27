using System;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.Timing;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Legacy <c>uscTimeEngine.ascx.cs</c> lines 183-207 / 224-250: the two time-setting limits the Time Engine screen
/// checked before queueing a run, now checked by <c>TimeEngineAppService.RunTimeEngine</c>. The end date may not run
/// more than <c>ManageTimeSettings.FutureEvaluationInDays</c> past the payroll control record's month end, the start
/// date may not precede <c>ManageTimeSettings.EarliestEvaluationDate</c>, and that date must be configured.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineAppService_TimeSettingsLimits_Tests : TimeEngineAppServiceTestBase
{
    [Fact]
    public async Task Should_Refuse_An_End_Date_Beyond_The_Future_Evaluation_Limit()
    {
        SetTimeSettings(futureEvaluationInDays: 30, earliestEvaluationDate: new DateTime(2020, 1, 1));
        CreatePayrollArea("TE-Limit-1", new DateTime(2024, 6, 30));

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput
            {
                PayrollAreaName = "TE-Limit-1",
                StartDate = new DateTime(2024, 7, 1),
                EndDate = new DateTime(2024, 7, 31)
            }));

        exception.Message.ShouldContain("30/07/2024");
        EnqueuedArgs.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Queue_A_Run_That_Ends_Exactly_On_The_Future_Evaluation_Limit()
    {
        SetTimeSettings(futureEvaluationInDays: 30, earliestEvaluationDate: new DateTime(2020, 1, 1));
        CreatePayrollArea("TE-Limit-2", new DateTime(2024, 6, 30));

        var output = await TimeEngineAppService.RunTimeEngine(new RunTimeEngineInput
        {
            PayrollAreaName = "TE-Limit-2",
            StartDate = new DateTime(2024, 7, 1),
            EndDate = new DateTime(2024, 7, 30)
        });

        output.Queued.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_A_Start_Date_Before_The_Earliest_Evaluation_Date()
    {
        SetTimeSettings(futureEvaluationInDays: 300, earliestEvaluationDate: new DateTime(2021, 4, 1));
        CreatePayrollArea("TE-Limit-3", new DateTime(2021, 4, 30));

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput
            {
                PayrollAreaName = "TE-Limit-3",
                StartDate = new DateTime(2021, 3, 31),
                EndDate = new DateTime(2021, 4, 30)
            }));

        exception.Message.ShouldContain("01/04/2021");
    }

    /// <summary>The same limits on the codes-only path, against the first employee's area's control record.</summary>
    [Fact]
    public async Task Should_Apply_The_Limits_To_A_Codes_Only_Run_Through_The_Employees_Area()
    {
        SetTimeSettings(futureEvaluationInDays: 30, earliestEvaluationDate: new DateTime(2020, 1, 1));
        var area = CreatePayrollArea("TE-Limit-4", new DateTime(2024, 6, 30));
        CreateEmployee("LIM1", area.Id);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput
            {
                EmployeeCodes = "LIM1",
                StartDate = new DateTime(2024, 7, 1),
                EndDate = new DateTime(2024, 7, 31)
            }));

        exception.Message.ShouldContain("30/07/2024");
    }

    /// <summary>
    /// Legacy refused the codes path with "Configuration for TIME_EVAL_EARLIEST_DATE not done." and silently ran
    /// nothing on the area path. Both refuse here, with that reason: a run that does not happen must say why.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_When_No_Earliest_Evaluation_Date_Is_Configured()
    {
        SetTimeSettings(futureEvaluationInDays: 300, earliestEvaluationDate: default(DateTime));
        CreatePayrollArea("TE-Limit-5", new DateTime(2024, 6, 30));

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput
            {
                PayrollAreaName = "TE-Limit-5",
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(2024, 6, 30)
            }));

        exception.Message.ShouldBe(Resolve<Abp.Localization.ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName,
            "TimeEngineEarliestEvaluationDateNotConfigured"));
        EnqueuedArgs.ShouldBeNull();
    }

    /// <summary>
    /// A blank Start Date runs from the 1st of the current month (legacy's own default), so that is the date the
    /// earliest-date limit is held against.
    /// </summary>
    [Fact]
    public async Task Should_Hold_A_Blank_Start_Date_As_The_First_Of_This_Month_Against_The_Earliest_Date()
    {
        var firstOfThisMonth = new DateTime(Clock.Now.Year, Clock.Now.Month, 1);
        SetTimeSettings(futureEvaluationInDays: 3650, earliestEvaluationDate: firstOfThisMonth.AddMonths(1));
        CreatePayrollArea("TE-Limit-6", firstOfThisMonth.AddMonths(1).AddDays(-1));

        await Should.ThrowAsync<UserFriendlyException>(() => TimeEngineAppService.RunTimeEngine(
            new RunTimeEngineInput
            {
                PayrollAreaName = "TE-Limit-6",
                StartDate = null,
                EndDate = firstOfThisMonth.AddMonths(1)
            }));
    }
}
