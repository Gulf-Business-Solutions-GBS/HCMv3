using System;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;
using HCMv3.PayrollManagement;
using HCMv3.ProjectManagement;
using HCMv3.ProjectManagement.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ProjectManagement;

/// <summary>
/// <see cref="ProjectCostEngineBackgroundJob"/> in isolation: what it hands the engine, and what it does with a
/// failure. It must NEVER throw -- ABP's job manager retries a job that throws, and a retried run is a second run
/// (the reason <c>PayrollEngineBackgroundJob</c> and <c>GoLivePayrollRunJob</c> never throw either).
/// </summary>
// ReSharper disable once InconsistentNaming
public class ProjectCostEngineBackgroundJob_Tests
{
    private const string Area = "PA";

    private readonly IProjectCostEngineService _engine = Substitute.For<IProjectCostEngineService>();
    private readonly IEngineRunProgressWriter _progress = Substitute.For<IEngineRunProgressWriter>();
    private readonly IUnitOfWorkManager _unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
    private readonly IAbpSession _session = Substitute.For<IAbpSession>();

    private ProjectCostEngineBackgroundJob BuildJob()
    {
        // Every member left unconfigured returns a substitute or a completed task, which is all the job needs
        // from the unit-of-work manager and the session here.
        return new ProjectCostEngineBackgroundJob(_engine, _unitOfWorkManager, _session, _progress);
    }

    private static ProjectCostEngineJobArgs Args() => new()
    {
        TenantId = 1,
        UserId = 2,
        CurrentUserName = "payroll.officer",
        PayrollRunType = PayrollRunType.Regular,
        EmployeeCodes = "E1",
        PayInDate = new DateTime(2024, 1, 31),
        PayrollAreaName = Area
    };

    [Fact]
    public async Task Should_Hand_The_Engine_The_Run_In_Batches_Of_Ten()
    {
        ProjectCostEngineRunInput handed = null;
        _engine.RunAsync(Arg.Do<ProjectCostEngineRunInput>(i => handed = i)).Returns(new ProjectCostEngineRunResult());

        await BuildJob().ExecuteAsync(Args());

        handed.ShouldNotBeNull();
        handed.CommitBatchSize.ShouldBe(ProjectCostEngineBatches.Size);
        ProjectCostEngineBatches.Size.ShouldBe(10);
        handed.PayrollAreaName.ShouldBe(Area);
        handed.EmployeeCodes.ShouldBe("E1");
        handed.PayInDate.ShouldBe(new DateTime(2024, 1, 31));
        handed.CurrentUserName.ShouldBe("payroll.officer");
    }

    /// <summary>The job runs as the person who pressed Run, around a NON-transactional unit of work.</summary>
    [Fact]
    public async Task Should_Run_As_The_User_Who_Started_It_Outside_One_Big_Transaction()
    {
        _engine.RunAsync(Arg.Any<ProjectCostEngineRunInput>()).Returns(new ProjectCostEngineRunResult());

        await BuildJob().ExecuteAsync(Args());

        _session.Received(1).Use(1, 2L);
        _unitOfWorkManager.Received(1).Begin(Arg.Is<UnitOfWorkOptions>(o => o.IsTransactional == false));
    }

    /// <summary>A failure before the run began has no progress row yet: one is opened and closed as Failed, with the reason.</summary>
    [Fact]
    public async Task Should_Not_Throw_And_Should_Record_The_Failure_On_A_Progress_Row()
    {
        _engine.RunAsync(Arg.Any<ProjectCostEngineRunInput>())
            .ThrowsAsync(new Abp.UI.UserFriendlyException("Payroll Control Record not locked or is not current."));
        _progress.BeginRunAsync(Arg.Any<EngineRunStartInfo>()).Returns(55L);

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));

        await _progress.Received(1).BeginRunAsync(Arg.Is<EngineRunStartInfo>(i =>
            i.EngineType == EngineType.ProjectCostEngine
            && i.RunKey == ProjectCostEngineService.RunKey
            && i.StartedByUserName == "payroll.officer"
            && i.Parameters.Contains("Payroll Area : PA")));
        await _progress.Received(1).CompleteRunAsync(
            55L, EngineRunStatus.Failed, null, "Payroll Control Record not locked or is not current.");
    }

    /// <summary>A failure during the run is already on the run's own row (the engine closed it as Failed): no second row.</summary>
    [Fact]
    public async Task Should_Not_Record_A_Second_Row_For_A_Failure_The_Engine_Already_Recorded()
    {
        var failure = new InvalidOperationException("the database went away");
        failure.Data[ProjectCostEngineService.RunFailureRecordedKey] = true;
        _engine.RunAsync(Arg.Any<ProjectCostEngineRunInput>()).ThrowsAsync(failure);

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));

        await _progress.DidNotReceive().BeginRunAsync(Arg.Any<EngineRunStartInfo>());
    }

    /// <summary>
    /// Refused because another run is going: a Failed row would become the latest run and hide that run's progress,
    /// so nothing is written -- the refusal is only logged.
    /// </summary>
    [Fact]
    public async Task Should_Not_Hide_A_Run_In_Progress_Behind_Its_Refusal()
    {
        _engine.RunAsync(Arg.Any<ProjectCostEngineRunInput>())
            .ThrowsAsync(new Abp.UI.UserFriendlyException("The Project Cost Engine is already running"));
        _progress.GetActiveRunAsync(EngineType.ProjectCostEngine, ProjectCostEngineService.RunKey)
            .Returns(new ActiveEngineRun { EngineRunId = 9, RunKey = ProjectCostEngineService.RunKey });

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));

        await _progress.DidNotReceive().BeginRunAsync(Arg.Any<EngineRunStartInfo>());
    }

    /// <summary>Even when the progress store itself fails, the job still does not throw.</summary>
    [Fact]
    public async Task Should_Not_Throw_When_The_Failure_Cannot_Be_Recorded_Either()
    {
        _engine.RunAsync(Arg.Any<ProjectCostEngineRunInput>()).ThrowsAsync(new Exception("boom"));
        _progress.BeginRunAsync(Arg.Any<EngineRunStartInfo>()).ThrowsAsync(new Exception("progress store down"));

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));
    }
}
