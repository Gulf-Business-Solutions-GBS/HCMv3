using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Timing;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.EngineRuns;

/// <summary>
/// <b>These tests exist to prove one thing: progress written while an outer unit of work is still open is
/// visible to a reader that is not part of that unit of work.</b>
///
/// <para>Both engines run inside a single <c>[UnitOfWork]</c> that completes only when the last employee is
/// done (see <c>tools/HCMv3.TimeEngineRunner/Program.cs</c>, one <c>uow.CompleteAsync()</c> around the whole
/// run). Anything written through the engine's own repositories -- <c>TimeLog</c>, <c>PayrollLog</c>,
/// <c>ExecutionLog</c> -- is therefore invisible until the run ends, which is exactly when progress stops
/// being useful. <see cref="Should_Publish_Progress_That_Is_Visible_While_The_Outer_UnitOfWork_Is_Still_Open"/>
/// asserts BOTH halves of that: the ordinary repository write is NOT visible, and the progress write IS.
/// Without the negative half the test would pass for the wrong reason.</para>
///
/// <para><b>What this harness can and cannot prove.</b> The test database is SQLite in-memory over a single
/// shared connection with <c>Configuration.UnitOfWork.IsTransactional = false</c>
/// (<c>HCMv3TestBaseModule</c> / <c>ServiceCollectionRegistrar</c>). So these tests prove the essential
/// property -- the progress row is FLUSHED AND COMMITTED to the database before the outer unit of work
/// completes, rather than sitting in the outer DbContext's change tracker -- but they cannot exercise SQL
/// Server transaction isolation, because the harness opens no transactions to isolate. On SQL Server the
/// same behaviour comes from ABP giving a <c>RequiresNew</c> unit of work its own transaction strategy, and
/// hence its own connection and transaction.</para>
/// </summary>
public class EngineRunProgressWriter_Tests : AppTestBase
{
    private readonly IEngineRunProgressWriter _writer;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public EngineRunProgressWriter_Tests()
    {
        _writer = Resolve<IEngineRunProgressWriter>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    /// <summary>
    /// THE test for this feature. If it ever fails, the progress bar has become a lie that only fills in
    /// once the run is over -- worse than showing nothing, because it looks like it works.
    /// </summary>
    [Fact]
    public async Task Should_Publish_Progress_That_Is_Visible_While_The_Outer_UnitOfWork_Is_Still_Open()
    {
        long runId;

        // The engine's outer unit of work: opened here, deliberately NEVER completed inside the assertions.
        using (var outerUow = _unitOfWorkManager.Begin())
        {
            // --- CONTROL -------------------------------------------------------------------------------
            // An ordinary repository write inside the outer unit of work. This is how the engines write
            // TimeLog/PayrollLog/ExecutionLog today. It must NOT be visible to an outside reader.
            var ordinaryRepository = Resolve<IRepository<EngineRun, long>>();
            await ordinaryRepository.InsertAsync(new EngineRun
            {
                TenantId = AbpSession.GetTenantId(),
                EngineType = EngineType.ProjectCostEngine, // a marker value no other assertion here uses
                RunKey = "control-row-written-inside-the-outer-uow",
                StartedAt = Clock.Now,
                LastHeartbeatAt = Clock.Now,
                Status = EngineRunStatus.Running
            });

            // --- THE THING UNDER TEST ------------------------------------------------------------------
            runId = await _writer.BeginRunAsync(new EngineRunStartInfo
            {
                EngineType = EngineType.TimeEngine,
                StartedByUserName = "operator",
                Parameters = "Start Date : 01/01/2026 | End Date : 31/01/2026"
            });

            runId.ShouldBeGreaterThan(0, "BeginRunAsync returns 0 only when progress could not be written at all.");

            await _writer.SetTotalEmployeesAsync(runId, 3);

            await _writer.RecordEmployeeAsync(new EngineRunEmployeeProgress
            {
                EngineRunId = runId,
                SequenceNo = 1,
                EmployeeId = 101,
                EmployeeCode = "E101",
                EmployeeName = "First Employee",
                Succeeded = true,
                Message = "Successfull",
                StartedAt = Clock.Now,
                CompletedAt = Clock.Now,
                ElapsedMilliseconds = 2000
            });

            await _writer.RecordEmployeeAsync(new EngineRunEmployeeProgress
            {
                EngineRunId = runId,
                SequenceNo = 2,
                EmployeeId = 102,
                EmployeeCode = "E102",
                EmployeeName = "Second Employee",
                Succeeded = false,
                Message = "Employee Sub Group not found for Employee.",
                StartedAt = Clock.Now,
                CompletedAt = Clock.Now,
                ElapsedMilliseconds = 4000
            });

            // --- ASSERT, WITH THE OUTER UNIT OF WORK STILL OPEN -----------------------------------------
            // UsingDbContextAsync resolves a fresh DbContext that is not the outer unit of work's, i.e. it
            // sees only what has actually reached the database. This models the polling status endpoint.
            await UsingDbContextAsync(async context =>
            {
                var controlRow = await context.EngineRuns
                    .FirstOrDefaultAsync(r => r.RunKey == "control-row-written-inside-the-outer-uow");

                controlRow.ShouldBeNull(
                    "A write made through the engine's own unit of work must NOT be visible mid-run -- " +
                    "if it were, this test would prove nothing about the progress writer.");

                var run = await context.EngineRuns.FirstOrDefaultAsync(r => r.Id == runId);

                run.ShouldNotBeNull("Progress must be readable WHILE the engine's unit of work is open.");
                run.Status.ShouldBe(EngineRunStatus.Running);
                run.TotalEmployees.ShouldBe(3);
                run.SucceededEmployees.ShouldBe(1);
                run.FailedEmployees.ShouldBe(1);
                run.TotalProcessingMilliseconds.ShouldBe(6000);

                var employees = await context.EngineRunEmployees
                    .Where(r => r.EngineRunId == runId)
                    .OrderBy(r => r.SequenceNo)
                    .ToListAsync();

                employees.Count.ShouldBe(2);
                employees[0].EmployeeCode.ShouldBe("E101");
                employees[0].Succeeded.ShouldBeTrue();
                employees[1].EmployeeCode.ShouldBe("E102");
                employees[1].Succeeded.ShouldBeFalse();
                employees[1].Message.ShouldBe("Employee Sub Group not found for Employee.");
            });

            await outerUow.CompleteAsync();
        }
    }

    /// <summary>
    /// The Stop button must be seen by a run whose transaction opened before the button was pressed --
    /// otherwise Stop silently does nothing, which is how operators end up killing processes.
    /// </summary>
    [Fact]
    public async Task Should_See_A_Stop_Request_Made_After_The_Run_Started()
    {
        var runId = await _writer.BeginRunAsync(new EngineRunStartInfo { EngineType = EngineType.PayrollEngine });

        using (var outerUow = _unitOfWorkManager.Begin())
        {
            (await _writer.IsStopRequestedAsync(runId)).ShouldBeFalse();

            // An operator presses Stop on a different connection while the run's transaction is open.
            await UsingDbContextAsync(async context =>
            {
                var run = await context.EngineRuns.FirstAsync(r => r.Id == runId);
                run.StopRequested = true;
            });

            (await _writer.IsStopRequestedAsync(runId)).ShouldBeTrue(
                "The engine reads the stop flag on its own connection precisely so a request made after " +
                "its transaction opened is still seen.");

            await outerUow.CompleteAsync();
        }
    }

    /// <summary>A crashed/killed run must not be left "in progress" forever.</summary>
    [Fact]
    public async Task Should_Abandon_A_Previous_Run_That_Stopped_Heartbeating()
    {
        // A run that "died": still Running, heartbeat long past the stale threshold.
        var deadRunId = await UsingDbContextAsync(async context =>
        {
            var run = new EngineRun
            {
                TenantId = AbpSession.GetTenantId(),
                EngineType = EngineType.TimeEngine,
                RunKey = "killed-run",
                StartedAt = Clock.Now.AddHours(-2),
                LastHeartbeatAt = Clock.Now.AddSeconds(-(EngineRunConsts.StaleHeartbeatSeconds + 60)),
                Status = EngineRunStatus.Running
            };

            context.EngineRuns.Add(run);
            await context.SaveChangesAsync();
            return run.Id;
        });

        // Starting the next run sweeps it up -- an operator never has to do anything to unblock themselves.
        await _writer.BeginRunAsync(new EngineRunStartInfo { EngineType = EngineType.TimeEngine });

        await UsingDbContextAsync(async context =>
        {
            var dead = await context.EngineRuns.FirstAsync(r => r.Id == deadRunId);

            dead.Status.ShouldBe(EngineRunStatus.Abandoned);
            dead.CompletedAt.ShouldNotBeNull();
            dead.CompletedAt.Value.ShouldBe(dead.LastHeartbeatAt,
                "The heartbeat is the last moment the process was demonstrably alive; anything later would be invented.");
            dead.FailureMessage.ShouldNotBeNullOrWhiteSpace();
        });
    }

    /// <summary>A healthy run of the same engine must never be swept up by the staleness sweep.</summary>
    [Fact]
    public async Task Should_Not_Abandon_A_Run_That_Is_Still_Heartbeating()
    {
        var liveRunId = await _writer.BeginRunAsync(new EngineRunStartInfo { EngineType = EngineType.PayrollEngine });

        await _writer.BeginRunAsync(new EngineRunStartInfo { EngineType = EngineType.PayrollEngine });

        await UsingDbContextAsync(async context =>
        {
            var live = await context.EngineRuns.FirstAsync(r => r.Id == liveRunId);
            live.Status.ShouldBe(EngineRunStatus.Running);
        });
    }

    [Fact]
    public async Task Should_Close_The_Run_On_Completion()
    {
        var runId = await _writer.BeginRunAsync(new EngineRunStartInfo { EngineType = EngineType.TimeEngine });

        await _writer.CompleteRunAsync(runId, EngineRunStatus.CompletedWithErrors, 4242, null);

        await UsingDbContextAsync(async context =>
        {
            var run = await context.EngineRuns.FirstAsync(r => r.Id == runId);

            run.Status.ShouldBe(EngineRunStatus.CompletedWithErrors);
            run.CompletedAt.ShouldNotBeNull();
            run.ExecutionLogId.ShouldBe(4242);
        });
    }

    /// <summary>Skipped employees get no grid row but must still shrink the remaining count.</summary>
    [Fact]
    public async Task Should_Count_Skipped_Employees_Without_Writing_A_Grid_Row()
    {
        var runId = await _writer.BeginRunAsync(new EngineRunStartInfo { EngineType = EngineType.TimeEngine });

        await _writer.RecordSkippedEmployeeAsync(runId);
        await _writer.RecordSkippedEmployeeAsync(runId);

        await UsingDbContextAsync(async context =>
        {
            var run = await context.EngineRuns.FirstAsync(r => r.Id == runId);
            run.SkippedEmployees.ShouldBe(2);

            (await context.EngineRunEmployees.CountAsync(r => r.EngineRunId == runId)).ShouldBe(0);
        });
    }

    /// <summary>
    /// A progress-store failure must never fail a payroll run: every member no-ops on the documented
    /// "progress unavailable" id of 0 rather than throwing.
    /// </summary>
    [Fact]
    public async Task Should_No_Op_When_Progress_Is_Unavailable()
    {
        await _writer.SetTotalEmployeesAsync(0, 10);
        await _writer.RecordSkippedEmployeeAsync(0);
        await _writer.HeartbeatAsync(0);
        await _writer.CompleteRunAsync(0, EngineRunStatus.Completed, null, null);
        await _writer.RecordEmployeeAsync(new EngineRunEmployeeProgress { EngineRunId = 0 });

        (await _writer.IsStopRequestedAsync(0)).ShouldBeFalse();
        (await _writer.BeginRunAsync(null)).ShouldBe(0);
    }

    /// <summary>Messages are truncated exactly as the engines truncate their own log rows.</summary>
    [Fact]
    public async Task Should_Truncate_Long_Messages()
    {
        var runId = await _writer.BeginRunAsync(new EngineRunStartInfo { EngineType = EngineType.PayrollEngine });

        await _writer.RecordEmployeeAsync(new EngineRunEmployeeProgress
        {
            EngineRunId = runId,
            SequenceNo = 1,
            EmployeeCode = "E1",
            Succeeded = false,
            Message = new string('x', EngineRunConsts.MaxMessageLength + 500),
            ElapsedMilliseconds = 1
        });

        await UsingDbContextAsync(async context =>
        {
            var row = await context.EngineRunEmployees.FirstAsync(r => r.EngineRunId == runId);
            row.Message.Length.ShouldBe(EngineRunConsts.MaxMessageLength);
        });
    }
}
