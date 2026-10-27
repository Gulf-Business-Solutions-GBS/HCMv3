using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Runtime.Session;
using Abp.Timing;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.EngineRuns;

/// <summary>
/// <see cref="EngineRunScope"/> on the shared reader: a screen can follow the runs of one run key (the Time Engine's
/// payroll area) and leave out runs by key prefix (the payroll engine's own time-engine calls). Without a scope the
/// reader behaves exactly as before -- the Payroll Engine screen depends on that.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EngineRunProgressReader_Scope_Tests : AppTestBase
{
    private readonly EngineRunProgressReader _reader;

    public EngineRunProgressReader_Scope_Tests()
    {
        _reader = Resolve<EngineRunProgressReader>();
    }

    [Fact]
    public async Task Should_Follow_The_Latest_Run_Of_Any_Key_Without_A_Scope()
    {
        await CreateRunAsync(EngineType.TimeEngine, "KEY-A");
        var latest = await CreateRunAsync(EngineType.TimeEngine, "HIDDEN:KEY-A");

        (await _reader.GetStatusAsync(EngineType.TimeEngine)).RunId.ShouldBe(latest);
        (await _reader.GetStatusAsync(EngineType.TimeEngine, null)).RunId.ShouldBe(latest);
    }

    [Fact]
    public async Task Should_Follow_The_Latest_Run_With_The_Scopes_Key()
    {
        var runA = await CreateRunAsync(EngineType.TimeEngine, "KEY-A");
        await CreateRunAsync(EngineType.TimeEngine, "KEY-B");

        var status = await _reader.GetStatusAsync(EngineType.TimeEngine, new EngineRunScope { RunKey = "KEY-A" });

        status.RunId.ShouldBe(runA);
    }

    [Fact]
    public async Task Should_Leave_Out_Runs_With_The_Excluded_Prefix()
    {
        var visible = await CreateRunAsync(EngineType.TimeEngine, "KEY-A");
        await CreateRunAsync(EngineType.TimeEngine, "HIDDEN:KEY-A");

        var status = await _reader.GetStatusAsync(
            EngineType.TimeEngine, new EngineRunScope { ExcludedRunKeyPrefix = "HIDDEN:" });

        status.RunId.ShouldBe(visible);
    }

    [Fact]
    public async Task Should_Say_No_Run_When_Nothing_Is_In_Scope()
    {
        await CreateRunAsync(EngineType.TimeEngine, "KEY-B");

        (await _reader.GetStatusAsync(EngineType.TimeEngine, new EngineRunScope { RunKey = "KEY-A" }))
            .HasRun.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Never_Cross_Into_The_Other_Engine()
    {
        await CreateRunAsync(EngineType.PayrollEngine, "KEY-A");

        (await _reader.GetStatusAsync(EngineType.TimeEngine, new EngineRunScope { RunKey = "KEY-A" }))
            .HasRun.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Page_The_Employees_Of_The_Run_In_Scope()
    {
        var runA = await CreateRunAsync(EngineType.TimeEngine, "KEY-A");
        var runB = await CreateRunAsync(EngineType.TimeEngine, "KEY-B");
        await AddEmployeeRowAsync(runA, 1, "A1");
        await AddEmployeeRowAsync(runB, 1, "B1");

        var rows = await _reader.GetEmployeesAsync(
            EngineType.TimeEngine,
            new GetEngineRunEmployeesInput { MaxResultCount = 10 },
            new EngineRunScope { RunKey = "KEY-A" });

        rows.Items.Select(r => r.EmployeeCode).ShouldBe(new[] { "A1" });
    }

    /// <summary>A run named by id is honoured whatever the scope -- an export of a run from the history.</summary>
    [Fact]
    public async Task Should_Honour_An_Explicit_Run_Id_Outside_The_Scope()
    {
        var runA = await CreateRunAsync(EngineType.TimeEngine, "KEY-A");
        await CreateRunAsync(EngineType.TimeEngine, "KEY-B");
        await AddEmployeeRowAsync(runA, 1, "A1");

        var rows = await _reader.GetEmployeesAsync(
            EngineType.TimeEngine,
            new GetEngineRunEmployeesInput { RunId = runA, MaxResultCount = 10 },
            new EngineRunScope { RunKey = "KEY-B" });

        rows.Items.Select(r => r.EmployeeCode).ShouldBe(new[] { "A1" });
    }

    [Fact]
    public async Task Should_Stop_Only_The_Running_Run_In_Scope()
    {
        var runA = await CreateRunAsync(EngineType.TimeEngine, "KEY-A");
        var runB = await CreateRunAsync(EngineType.TimeEngine, "KEY-B");

        (await _reader.RequestStopAsync(EngineType.TimeEngine, new EngineRunScope { RunKey = "KEY-A" })).ShouldBeTrue();

        await UsingDbContextAsync(async context =>
        {
            (await context.EngineRuns.FirstAsync(r => r.Id == runA)).StopRequested.ShouldBeTrue();
            (await context.EngineRuns.FirstAsync(r => r.Id == runB)).StopRequested.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Should_Report_Nothing_To_Stop_When_The_Scopes_Run_Is_Not_Running()
    {
        await CreateRunAsync(EngineType.TimeEngine, "KEY-A", EngineRunStatus.Completed);
        await CreateRunAsync(EngineType.TimeEngine, "KEY-B");

        (await _reader.RequestStopAsync(EngineType.TimeEngine, new EngineRunScope { RunKey = "KEY-A" })).ShouldBeFalse();
    }

    private Task<long> CreateRunAsync(EngineType engineType, string runKey, EngineRunStatus status = EngineRunStatus.Running)
    {
        return UsingDbContextAsync(async context =>
        {
            var run = new EngineRun
            {
                TenantId = AbpSession.GetTenantId(),
                EngineType = engineType,
                RunKey = runKey,
                Status = status,
                StartedAt = Clock.Now,
                LastHeartbeatAt = Clock.Now,
                CompletedAt = status == EngineRunStatus.Running ? null : Clock.Now
            };

            context.EngineRuns.Add(run);
            await context.SaveChangesAsync();
            return run.Id;
        });
    }

    private Task AddEmployeeRowAsync(long runId, int sequenceNo, string code)
    {
        return UsingDbContextAsync(async context =>
        {
            context.EngineRunEmployees.Add(new EngineRunEmployee
            {
                TenantId = AbpSession.GetTenantId(),
                EngineRunId = runId,
                SequenceNo = sequenceNo,
                EmployeeCode = code,
                EmployeeName = code + " Name",
                Succeeded = true,
                Message = "Successfull",
                StartedAt = Clock.Now,
                CompletedAt = Clock.Now,
                ElapsedMilliseconds = 1000
            });

            await context.SaveChangesAsync();
        });
    }
}
