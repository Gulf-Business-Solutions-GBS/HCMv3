using System;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// <see cref="PayrollEngineBackgroundJob"/> in isolation: what it hands the engine, and what it does with a
/// failure. It must NEVER throw -- ABP's job manager retries a job that throws, and a retried payroll run is a
/// second payroll run (the reason <c>GoLivePayrollRunJob</c> never throws either).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollEngineBackgroundJob_Tests
{
    private const string Area = "PA";

    private readonly IPayrollEngineService _engine = Substitute.For<IPayrollEngineService>();
    private readonly IEngineRunProgressWriter _progress = Substitute.For<IEngineRunProgressWriter>();

    private PayrollEngineBackgroundJob BuildJob()
    {
        // Every member left unconfigured returns a substitute or a completed task, which is all the job needs
        // from the unit-of-work manager and the session here.
        return new PayrollEngineBackgroundJob(
            _engine,
            Substitute.For<IUnitOfWorkManager>(),
            Substitute.For<IAbpSession>(),
            _progress);
    }

    private static PayrollEngineJobArgs Args(PayrollRegisterTarget target = PayrollRegisterTarget.Live) => new()
    {
        TenantId = 1,
        UserId = 2,
        PayrollRunType = PayrollRunType.Regular,
        PayInDate = new DateTime(2026, 9, 30),
        PayrollAreaName = Area,
        RegisterTarget = target,
    };

    [Fact]
    public async Task Should_Hand_The_Engine_The_Run_In_Batches_Of_Ten()
    {
        PayrollEngineRunInput handed = null;
        _engine.RunAsync(Arg.Do<PayrollEngineRunInput>(i => handed = i)).Returns(new PayrollEngineRunResult());

        await BuildJob().ExecuteAsync(Args(PayrollRegisterTarget.Simulation));

        handed.ShouldNotBeNull();
        handed.CommitBatchSize.ShouldBe(PayrollEngineBatches.Size);
        handed.RegisterTarget.ShouldBe(PayrollRegisterTarget.Simulation);
        handed.PayrollAreaName.ShouldBe(Area);
        handed.PayInDate.ShouldBe(new DateTime(2026, 9, 30));
    }

    /// <summary>A failure before the run began has no progress row yet: one is opened and closed as Failed, with the reason.</summary>
    [Fact]
    public async Task Should_Not_Throw_And_Should_Record_The_Failure_On_A_Progress_Row()
    {
        _engine.RunAsync(Arg.Any<PayrollEngineRunInput>())
            .ThrowsAsync(new Abp.UI.UserFriendlyException("Payroll Calender does not have record for Payroll month you are running."));
        _progress.BeginRunAsync(Arg.Any<EngineRunStartInfo>()).Returns(55L);

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));

        await _progress.Received(1).BeginRunAsync(Arg.Is<EngineRunStartInfo>(i =>
            i.EngineType == EngineType.PayrollEngine && i.RunKey == PayrollEngineService.BuildRunKey(Area)));
        await _progress.Received(1).CompleteRunAsync(
            55L, EngineRunStatus.Failed, null, "Payroll Calender does not have record for Payroll month you are running.");
    }

    /// <summary>A failure during the run is already on the run's own row (the engine closed it as Failed): no second row.</summary>
    [Fact]
    public async Task Should_Not_Record_A_Second_Row_For_A_Failure_The_Engine_Already_Recorded()
    {
        var failure = new InvalidOperationException("the database went away");
        failure.Data[PayrollEngineService.RunFailureRecordedKey] = true;
        _engine.RunAsync(Arg.Any<PayrollEngineRunInput>()).ThrowsAsync(failure);

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));

        await _progress.DidNotReceive().BeginRunAsync(Arg.Any<EngineRunStartInfo>());
    }

    /// <summary>
    /// Refused because the area is already running: a Failed row would become the latest run and hide that run's
    /// progress, so nothing is written -- the refusal is only logged.
    /// </summary>
    [Fact]
    public async Task Should_Not_Hide_A_Run_In_Progress_Behind_Its_Refusal()
    {
        _engine.RunAsync(Arg.Any<PayrollEngineRunInput>())
            .ThrowsAsync(new Abp.UI.UserFriendlyException("Payroll is already running for payroll area 'PA'"));
        _progress.GetActiveRunAsync(EngineType.PayrollEngine, PayrollEngineService.BuildRunKey(Area))
            .Returns(new ActiveEngineRun { EngineRunId = 9, RunKey = PayrollEngineService.BuildRunKey(Area) });

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));

        await _progress.DidNotReceive().BeginRunAsync(Arg.Any<EngineRunStartInfo>());
    }

    /// <summary>Even when the progress store itself fails, the job still does not throw.</summary>
    [Fact]
    public async Task Should_Not_Throw_When_The_Failure_Cannot_Be_Recorded_Either()
    {
        _engine.RunAsync(Arg.Any<PayrollEngineRunInput>()).ThrowsAsync(new Exception("boom"));
        _progress.BeginRunAsync(Arg.Any<EngineRunStartInfo>()).ThrowsAsync(new Exception("progress store down"));

        await Should.NotThrowAsync(() => BuildJob().ExecuteAsync(Args()));
    }
}
