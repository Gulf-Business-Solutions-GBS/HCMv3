using System;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// <see cref="TimeEngineBackgroundJob"/> in isolation: what it hands the engine, and what it does with a failure.
/// NO AUTOMATIC RETRY -- it must never throw: ABP's job manager retries a job that throws, and a retried Time Engine
/// run is a second run of the area nobody asked for. Same contract as <c>PayrollEngineBackgroundJob</c>. Legacy's
/// <c>Main</c> swallowed too: its outermost catch wrote a failed TimeLog row (Program.cs lines 185-199).
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineBackgroundJob_Tests
{
    private const string Area = "Area A";

    private readonly ITimeEngineService _engine = Substitute.For<ITimeEngineService>();
    private readonly IEngineRunProgressWriter _progress = Substitute.For<IEngineRunProgressWriter>();
    private readonly IAbpSession _session = Substitute.For<IAbpSession>();

    private TimeEngineBackgroundJob BuildJob()
    {
        // Unconfigured members return a substitute or a completed task -- all the job needs from the unit-of-work
        // manager here.
        return new TimeEngineBackgroundJob(_engine, Substitute.For<IUnitOfWorkManager>(), _session, _progress);
    }

    private static TimeEngineJobArgs Args(string payrollAreaName = Area) => new()
    {
        TenantId = 1,
        UserId = 2,
        PayrollAreaName = payrollAreaName,
        StartDate = new DateTime(2026, 9, 1),
        EndDate = new DateTime(2026, 9, 30),
        CurrentUserName = "operator",
    };

    [Fact]
    public async Task Should_Hand_The_Engine_The_Run_In_Batches_Of_Ten()
    {
        TimeEngineRunInput handed = null;
        _engine.CreateTimeRegisterAsync(Arg.Do<TimeEngineRunInput>(i => handed = i)).Returns(new TimeEngineRunResult());

        await BuildJob().ExecuteAsync(Args());

        handed.ShouldNotBeNull();
        handed.CommitBatchSize.ShouldBe(TimeEngineBatches.Size);
        handed.PayrollAreaName.ShouldBe(Area);
        handed.StartDate.ShouldBe(new DateTime(2026, 9, 1));
        handed.EndDate.ShouldBe(new DateTime(2026, 9, 30));
        handed.CurrentUserName.ShouldBe("operator");
        handed.IsRanFromPayrollEngine.ShouldBeFalse();
    }

    /// <summary>The job runs as the person who started it: the batches and the progress writer read the session.</summary>
    [Fact]
    public async Task Should_Run_As_The_Tenant_And_User_Who_Started_It()
    {
        _engine.CreateTimeRegisterAsync(Arg.Any<TimeEngineRunInput>()).Returns(new TimeEngineRunResult());

        await BuildJob().ExecuteAsync(Args());

        _session.Received(1).Use(1, 2L);
    }

    /// <summary>A failure before the run began has no progress row yet: one is opened and closed as Failed, with the reason.</summary>
    [Fact]
    public async Task Should_Not_Throw_And_Should_Record_The_Failure_On_A_Progress_Row()
    {
        _engine.CreateTimeRegisterAsync(Arg.Any<TimeEngineRunInput>())
            .ThrowsAsync(new ArgumentException("No End Date defined for Time Calculation or Check Date format."));
        _progress.BeginRunAsync(Arg.Any<EngineRunStartInfo>()).Returns(55L);

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));

        await _progress.Received(1).BeginRunAsync(Arg.Is<EngineRunStartInfo>(i =>
            i.EngineType == EngineType.TimeEngine
            && i.RunKey == TimeEngineRunKeys.ForPayrollArea(Area)
            && i.Parameters.Contains("Payroll Area : " + Area)));
        await _progress.Received(1).CompleteRunAsync(
            55L, EngineRunStatus.Failed, null, "No End Date defined for Time Calculation or Check Date format.");
    }

    /// <summary>A failure during the run is already on the run's own row (the engine closed it as Failed): no second row.</summary>
    [Fact]
    public async Task Should_Not_Record_A_Second_Row_For_A_Failure_The_Engine_Already_Recorded()
    {
        var failure = new InvalidOperationException("the database went away");
        failure.Data[TimeEngineService.RunFailureRecordedKey] = true;
        _engine.CreateTimeRegisterAsync(Arg.Any<TimeEngineRunInput>()).ThrowsAsync(failure);

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));

        await _progress.DidNotReceive().BeginRunAsync(Arg.Any<EngineRunStartInfo>());
    }

    /// <summary>
    /// Refused because the area is already running (two jobs queued at once): a Failed row would become that area's
    /// latest run and hide the running one's progress, so nothing is written -- the refusal is only logged.
    /// </summary>
    [Fact]
    public async Task Should_Not_Hide_A_Run_In_Progress_Behind_Its_Refusal()
    {
        _engine.CreateTimeRegisterAsync(Arg.Any<TimeEngineRunInput>())
            .ThrowsAsync(new UserFriendlyException("The Time Engine is already running for payroll area 'Area A'"));
        _progress.GetActiveRunAsync(EngineType.TimeEngine, TimeEngineRunKeys.ForPayrollArea(Area))
            .Returns(new ActiveEngineRun { EngineRunId = 9, RunKey = TimeEngineRunKeys.ForPayrollArea(Area) });

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));

        await _progress.DidNotReceive().BeginRunAsync(Arg.Any<EngineRunStartInfo>());
    }

    /// <summary>Even when the progress store itself fails, the job still does not throw.</summary>
    [Fact]
    public async Task Should_Not_Throw_When_The_Failure_Cannot_Be_Recorded_Either()
    {
        _engine.CreateTimeRegisterAsync(Arg.Any<TimeEngineRunInput>()).ThrowsAsync(new Exception("boom"));
        _progress.BeginRunAsync(Arg.Any<EngineRunStartInfo>()).ThrowsAsync(new Exception("progress store down"));

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));
    }

    /// <summary>The nightly autorun has no user; the job still runs it, as nobody in particular.</summary>
    [Fact]
    public async Task Should_Run_An_Autorun_Job_Without_A_User()
    {
        _engine.CreateTimeRegisterAsync(Arg.Any<TimeEngineRunInput>()).Returns(new TimeEngineRunResult());
        var args = Args();
        args.UserId = null;

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(args));

        _session.Received(1).Use(1, null);
        await _engine.Received(1).CreateTimeRegisterAsync(Arg.Any<TimeEngineRunInput>());
    }
}
