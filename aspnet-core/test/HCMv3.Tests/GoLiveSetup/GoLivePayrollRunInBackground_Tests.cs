using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.Threading;
using HCMv3.EngineRuns;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// Running a real company's payroll: 186 employees took half an hour inside one browser request, slowing as
/// it went, and a locked screen lost both the run and any way to see how it ended.
///
/// <para>So the payroll month is checked before either engine starts; the engines run in small batches, each
/// saved on its own, so the tenth batch is as quick as the first; and the screen starts the run in the
/// background and reads its progress from the server, where a reload or a new login can find it again.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLivePayrollRunInBackground_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime September = new(2026, 9, 30);
    private static readonly DateTime August = new(2026, 8, 31);
    private static readonly DateTime Joined = new(2020, 1, 15);

    private readonly IGoLiveSetupAppService _goLive;

    public GoLivePayrollRunInBackground_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- the month, before anything runs

    /// <summary>
    /// The payroll engine refuses a month its control record is not for -- but only after the time engine has
    /// worked out every employee's days. The same check now comes first, so nobody waits for a refusal.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_A_Month_The_Control_Record_Is_Not_For_Before_Any_Engine_Runs()
    {
        await GivenEmployeesPaidFor(September, "E001", "E002");
        var timeRunsBefore = EngineRuns(EngineType.TimeEngine);

        var run = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = August, PayrollAreaName = DefaultArea });

        run.Succeeded.ShouldBeFalse();
        run.StoppedAt.ShouldBe(GoLiveStoppedAt.MonthSetUp);
        run.Message.ShouldBe(Localized("GoLiveControlRecordNotCurrent", "September 2026", "August 2026"));
        EngineRuns(EngineType.TimeEngine).ShouldBe(timeRunsBefore);
    }

    // ---------------------------------------------------------------- batches

    /// <summary>Employees go through the engines a few at a time, each batch saved on its own.</summary>
    [Fact]
    public async Task Should_Run_The_Engines_In_Batches()
    {
        Resolve<GoLiveEngineBatches>().Size = 2;
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load("E001", "E002", "E003"));

        var run = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });

        run.Succeeded.ShouldBeTrue(run.Message);
        run.Employees.Count.ShouldBe(3);
        // Three employees in batches of two: two runs of each engine.
        EngineRuns(EngineType.TimeEngine).ShouldBe(2);
        EngineRuns(EngineType.PayrollEngine).ShouldBe(2);
        (await _goLive.GetPayrollResults(new GoLivePayrollResultsInput { PayDate = September, PayrollAreaName = DefaultArea }))
            .EmployeesPaid.ShouldBe(3);
    }

    // ---------------------------------------------------------------- in the background

    [Fact]
    public async Task Should_Report_No_Run_Before_One_Is_Started()
    {
        GivenTheReferenceData();

        var progress = await _goLive.GetPayrollRunProgress(DefaultArea);

        progress.Status.ShouldBe(GoLiveRunStatus.None);
    }

    /// <summary>Start returns at once; the run happens in a job, and its progress and result are read back.</summary>
    [Fact]
    public async Task Should_Run_Payroll_In_The_Background_And_Report_How_It_Ended()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load("E001", "E002"));

        var started = await _goLive.StartPayrollRun(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });

        started.Status.ShouldBe(GoLiveRunStatus.Queued);
        started.PayInDate.ShouldBe(September);
        started.TotalEmployees.ShouldBe(2);

        await RunTheQueuedJob();

        var finished = await _goLive.GetPayrollRunProgress(DefaultArea);
        finished.Status.ShouldBe(GoLiveRunStatus.Completed);
        finished.Result.ShouldNotBeNull();
        finished.Result.Succeeded.ShouldBeTrue(finished.Result.Message);
        finished.PayrollDone.ShouldBe(2);
        finished.TimeDone.ShouldBe(2);
    }

    /// <summary>A refusal that needs no engine comes back from Start itself, and nothing is queued.</summary>
    [Fact]
    public async Task Should_Answer_A_Refusal_At_Once_Without_Queuing_A_Run()
    {
        await GivenEmployeesPaidFor(September, "E001");

        var started = await _goLive.StartPayrollRun(new GoLiveRunPayrollInput { PayDate = August, PayrollAreaName = DefaultArea });

        started.Status.ShouldBe(GoLiveRunStatus.Completed);
        started.Result.StoppedAt.ShouldBe(GoLiveStoppedAt.MonthSetUp);
        UsingDbContext(context => context.BackgroundJobs.Count().ShouldBe(0));
    }

    [Fact]
    public async Task Should_Refuse_To_Start_A_Second_Run_While_One_Is_Going()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load("E001"));
        await _goLive.StartPayrollRun(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });

        var second = await _goLive.StartPayrollRun(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });

        second.Status.ShouldBe(GoLiveRunStatus.Queued);
        UsingDbContext(context => context.BackgroundJobs.Count().ShouldBe(1));
    }

    /// <summary>Stop is honoured between batches: what ran stays saved, and the result says it was stopped.</summary>
    [Fact]
    public async Task Should_Stop_Between_Batches_When_Asked()
    {
        Resolve<GoLiveEngineBatches>().Size = 1;
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load("E001", "E002"));
        await _goLive.StartPayrollRun(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });

        await _goLive.StopPayrollRun();
        await RunTheQueuedJob();

        var progress = await _goLive.GetPayrollRunProgress(DefaultArea);
        progress.Status.ShouldBe(GoLiveRunStatus.Stopped);
        progress.Result.Succeeded.ShouldBeFalse();
        progress.Result.Message.ShouldBe(Localized("GoLiveRunStoppedByUser"));
    }

    // ---------------------------------------------------------------- clear

    /// <summary>A run still writing registers would put back part of what Clear removes, so Clear waits for it.</summary>
    [Fact]
    public async Task Should_Refuse_To_Clear_While_A_Run_Is_Going()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load("E001"));
        await _goLive.StartPayrollRun(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });

        var refusal = await Should.ThrowAsync<Abp.UI.UserFriendlyException>(() =>
            _goLive.ClearCompany(new GoLiveClearCompanyInput { PayrollAreaName = DefaultArea, ConfirmCompanyName = "ABC Holding" }));

        refusal.Message.ShouldBe(Localized("GoLiveClearWhileRunning"));
    }

    /// <summary>After Clear the last run described employees that are gone, so the screen is told of no run.</summary>
    [Fact]
    public async Task Should_Forget_The_Last_Run_When_The_Company_Is_Cleared()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load("E001"));
        await _goLive.StartPayrollRun(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        await RunTheQueuedJob();

        await _goLive.ClearCompany(new GoLiveClearCompanyInput { PayrollAreaName = DefaultArea, ConfirmCompanyName = "ABC Holding" });

        (await _goLive.GetPayrollRunProgress(DefaultArea)).Status.ShouldBe(GoLiveRunStatus.None);
    }

    // ---------------------------------------------------------------- arrange

    /// <summary>What the job manager would do: take the queued job's arguments and run it.</summary>
    private async Task RunTheQueuedJob()
    {
        var args = UsingDbContext(context => context.BackgroundJobs.Single().JobArgs);
        var job = Resolve<GoLivePayrollRunJob>();
        await job.ExecuteAsync(Newtonsoft.Json.JsonConvert.DeserializeObject<GoLivePayrollRunJobArgs>(args));
    }

    private async Task GivenEmployeesPaidFor(DateTime monthEnd, params string[] codes)
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(codes));
        var run = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = monthEnd, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);
    }

    private int EngineRuns(EngineType type) =>
        UsingDbContext(context => context.EngineRuns.Count(r => r.TenantId == TenantId && r.EngineType == type));

    private void GivenTheReferenceData()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }
    }

    private static GoLiveLoadEmployeesInput Load(params string[] codes) =>
        new()
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = DefaultArea,
            Employees = codes
                .Select((code, i) => new GoLiveEmployeeRowDto
                {
                    Code = code,
                    FullName = "Employee " + code,
                    JoiningDate = Joined,
                    CivilId = "29001150" + (1000 + i),
                    BasicSalary = 500,
                })
                .ToList(),
        };

    private string Localized(string key, params object[] args) =>
        string.Format(Resolve<ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName, key), args);
}
