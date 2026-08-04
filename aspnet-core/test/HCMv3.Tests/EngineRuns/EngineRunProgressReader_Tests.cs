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
/// Covers the read side that the operator screen binds to: the summary DTO's arithmetic (counts, measured
/// average, ETA, elapsed), the per-employee grid, and the Stop / Clear / age-out actions.
/// </summary>
public class EngineRunProgressReader_Tests : AppTestBase
{
    private readonly EngineRunProgressReader _reader;

    public EngineRunProgressReader_Tests()
    {
        _reader = Resolve<EngineRunProgressReader>();
    }

    [Fact]
    public async Task Should_Report_An_Empty_State_When_The_Engine_Has_Never_Run()
    {
        var status = await _reader.GetStatusAsync(EngineType.TimeEngine);

        status.HasRun.ShouldBeFalse("The UI must render 'no runs yet', not a 0% progress bar.");
        status.EngineType.ShouldBe(EngineType.TimeEngine);
        status.StaleThresholdSeconds.ShouldBe(EngineRunConsts.StaleHeartbeatSeconds);
        status.AverageSecondsPerEmployee.ShouldBeNull();
        status.EstimatedSecondsRemaining.ShouldBeNull();
    }

    /// <summary>
    /// The core arithmetic an operator reads off the screen: how many are done, how many failed, the
    /// MEASURED average, and the estimated time remaining derived from it.
    /// </summary>
    [Fact]
    public async Task Should_Compute_Counts_Measured_Average_And_Estimate_For_A_Run_In_Flight()
    {
        // 10 employees; 4 done (3 ok, 1 failed) taking 20s in total; 1 skipped => 5 remaining.
        var runId = await CreateRunAsync(
            EngineType.PayrollEngine,
            status: EngineRunStatus.Running,
            total: 10,
            succeeded: 3,
            failed: 1,
            skipped: 1,
            totalProcessingMs: 20000,
            startedAt: Clock.Now.AddMinutes(-2),
            lastHeartbeatAt: Clock.Now);

        await AddEmployeeRowAsync(runId, 4, "E004", "Last Processed", succeeded: false, message: "boom", elapsedMs: 8000);

        var status = await _reader.GetStatusAsync(EngineType.PayrollEngine);

        status.HasRun.ShouldBeTrue();
        status.IsRunning.ShouldBeTrue();
        status.IsStale.ShouldBeFalse();
        status.TotalEmployees.ShouldBe(10);
        status.ProcessedEmployees.ShouldBe(4);
        status.SucceededEmployees.ShouldBe(3);
        status.FailedEmployees.ShouldBe(1);
        status.SkippedEmployees.ShouldBe(1);
        status.RemainingEmployees.ShouldBe(5);
        status.ProgressPercent.ShouldBe(50);

        // 20 000 ms over 4 completed employees = 5 s each. Measured, not assumed.
        status.AverageSecondsPerEmployee.ShouldBe(5);

        // 5 remaining x 5 s. An ESTIMATE, and labelled as one in the DTO.
        status.EstimatedSecondsRemaining.ShouldBe(25);
        status.EstimatedCompletionTime.ShouldNotBeNull();

        // Wall clock, not the sum of per-employee timings (which excludes setup/finalisation).
        status.ElapsedSeconds.ShouldBeGreaterThanOrEqualTo(119);

        status.CurrentEmployee.ShouldBe("E004 - Last Processed");
    }

    [Fact]
    public async Task Should_Not_Estimate_Before_Any_Employee_Has_Completed()
    {
        await CreateRunAsync(EngineType.TimeEngine, EngineRunStatus.Running, total: 50);

        var status = await _reader.GetStatusAsync(EngineType.TimeEngine);

        status.AverageSecondsPerEmployee.ShouldBeNull("Null means 'estimating...', 0 would be a lie.");
        status.EstimatedSecondsRemaining.ShouldBeNull();
        status.EstimatedCompletionTime.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Report_Actual_Elapsed_And_Drop_The_Estimate_Once_Finished()
    {
        var startedAt = Clock.Now.AddMinutes(-30);

        await CreateRunAsync(
            EngineType.TimeEngine,
            status: EngineRunStatus.Completed,
            total: 4,
            succeeded: 4,
            totalProcessingMs: 8000,
            startedAt: startedAt,
            lastHeartbeatAt: startedAt.AddMinutes(25),
            completedAt: startedAt.AddMinutes(25));

        var status = await _reader.GetStatusAsync(EngineType.TimeEngine);

        status.IsRunning.ShouldBeFalse();
        status.RemainingEmployees.ShouldBe(0);
        status.ProgressPercent.ShouldBe(100);
        status.ElapsedSeconds.ShouldBe(25 * 60, 1d);
        status.AverageSecondsPerEmployee.ShouldBe(2, "The average stays visible after the run for post-mortems.");
        status.EstimatedSecondsRemaining.ShouldBeNull("Nothing left to estimate.");
    }

    /// <summary>A crashed run must surface as stale rather than as a spinner that never stops.</summary>
    [Fact]
    public async Task Should_Flag_A_Run_That_Stopped_Heartbeating_As_Stale()
    {
        var lastHeartbeat = Clock.Now.AddSeconds(-(EngineRunConsts.StaleHeartbeatSeconds + 120));

        await CreateRunAsync(
            EngineType.PayrollEngine,
            status: EngineRunStatus.Running,
            total: 100,
            succeeded: 10,
            totalProcessingMs: 10000,
            startedAt: Clock.Now.AddHours(-1),
            lastHeartbeatAt: lastHeartbeat);

        var status = await _reader.GetStatusAsync(EngineType.PayrollEngine);

        status.IsStale.ShouldBeTrue();
        status.IsRunning.ShouldBeFalse("A stale run must not drive a live spinner.");
        status.Status.ShouldBe(EngineRunStatus.Running, "The persisted value is untouched until it is aged out.");
        status.SecondsSinceHeartbeat.ShouldBeGreaterThan(EngineRunConsts.StaleHeartbeatSeconds);
        status.EstimatedSecondsRemaining.ShouldBeNull("Estimating completion for a dead run would be nonsense.");

        // Elapsed stops at the last proof of life rather than growing forever.
        status.ElapsedSeconds.ShouldBeLessThan((Clock.Now - Clock.Now.AddHours(-1)).TotalSeconds);
    }

    [Fact]
    public async Task Should_Age_Out_Stale_Runs_On_Demand()
    {
        var runId = await CreateRunAsync(
            EngineType.TimeEngine,
            status: EngineRunStatus.Running,
            lastHeartbeatAt: Clock.Now.AddSeconds(-(EngineRunConsts.StaleHeartbeatSeconds + 1)));

        (await _reader.AbandonStaleRunsAsync(EngineType.TimeEngine)).ShouldBe(1);

        await UsingDbContextAsync(async context =>
        {
            var run = await context.EngineRuns.FirstAsync(r => r.Id == runId);
            run.Status.ShouldBe(EngineRunStatus.Abandoned);
        });
    }

    [Fact]
    public async Task Should_Return_The_Latest_Run_Only()
    {
        await CreateRunAsync(EngineType.TimeEngine, EngineRunStatus.Completed, total: 1);
        var newestId = await CreateRunAsync(EngineType.TimeEngine, EngineRunStatus.Running, total: 2);

        // A run of the OTHER engine must never leak into this engine's screen.
        await CreateRunAsync(EngineType.PayrollEngine, EngineRunStatus.Running, total: 999);

        var status = await _reader.GetStatusAsync(EngineType.TimeEngine);

        status.RunId.ShouldBe(newestId);
        status.TotalEmployees.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Page_And_Filter_The_Employee_Grid()
    {
        var runId = await CreateRunAsync(EngineType.PayrollEngine, EngineRunStatus.Running, total: 3);

        await AddEmployeeRowAsync(runId, 1, "E001", "Alice", true, "Successfull", 1000);
        await AddEmployeeRowAsync(runId, 2, "E002", "Bob", false, "Payroll Control Record is locked.", 2500);
        await AddEmployeeRowAsync(runId, 3, "E003", "Carol", true, "Successfull", 500);

        // Default: every employee, succeeded and failed alike, in processing order.
        var all = await _reader.GetEmployeesAsync(EngineType.PayrollEngine, new GetEngineRunEmployeesInput());
        all.TotalCount.ShouldBe(3);
        all.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "E001", "E002", "E003" });
        all.Items[1].Message.ShouldBe("Payroll Control Record is locked.");
        all.Items[1].ElapsedSeconds.ShouldBe(2.5);

        // Failures only -- what an operator wants the moment a run finishes.
        var failed = await _reader.GetEmployeesAsync(
            EngineType.PayrollEngine, new GetEngineRunEmployeesInput { OnlyFailed = true });
        failed.TotalCount.ShouldBe(1);
        failed.Items[0].EmployeeCode.ShouldBe("E002");

        // Free-text filter spans code, name and message.
        var filtered = await _reader.GetEmployeesAsync(
            EngineType.PayrollEngine, new GetEngineRunEmployeesInput { Filter = "locked" });
        filtered.TotalCount.ShouldBe(1);
        filtered.Items[0].EmployeeCode.ShouldBe("E002");

        var paged = await _reader.GetEmployeesAsync(
            EngineType.PayrollEngine, new GetEngineRunEmployeesInput { MaxResultCount = 2, SkipCount = 2 });
        paged.TotalCount.ShouldBe(3);
        paged.Items.Count.ShouldBe(1);
        paged.Items[0].EmployeeCode.ShouldBe("E003");
    }

    [Fact]
    public async Task Should_Not_Serve_Another_Engines_Run_To_The_Grid()
    {
        var timeRunId = await CreateRunAsync(EngineType.TimeEngine, EngineRunStatus.Running, total: 1);
        await AddEmployeeRowAsync(timeRunId, 1, "E001", "Alice", true, "Successfull", 100);

        var result = await _reader.GetEmployeesAsync(
            EngineType.PayrollEngine, new GetEngineRunEmployeesInput { RunId = timeRunId });

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Request_Stop_On_The_Running_Run()
    {
        await CreateRunAsync(EngineType.TimeEngine, EngineRunStatus.Completed);
        var runningId = await CreateRunAsync(EngineType.TimeEngine, EngineRunStatus.Running);

        (await _reader.RequestStopAsync(EngineType.TimeEngine)).ShouldBeTrue();

        await UsingDbContextAsync(async context =>
        {
            (await context.EngineRuns.FirstAsync(r => r.Id == runningId)).StopRequested.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_Report_No_Stop_Target_When_Nothing_Is_Running()
    {
        await CreateRunAsync(EngineType.TimeEngine, EngineRunStatus.Completed);

        (await _reader.RequestStopAsync(EngineType.TimeEngine)).ShouldBeFalse();
    }

    /// <summary>
    /// Clearing history must never destroy the counters of a run that is still going.
    ///
    /// <para>Note the <c>IsDeleted</c> assertions: these entities are <c>FullAuditedEntity</c>, so Clear is a
    /// SOFT delete -- identical to <c>ClearProjectCostEngineLog</c>, which also deletes through the
    /// repository rather than issuing legacy's <c>TRUNCATE TABLE</c>. The rows vanish from every query ABP
    /// serves (the soft-delete filter) but remain on disk.</para>
    /// </summary>
    [Fact]
    public async Task Should_Clear_Finished_Runs_But_Keep_The_Run_In_Flight()
    {
        var finishedId = await CreateRunAsync(EngineType.PayrollEngine, EngineRunStatus.CompletedWithErrors);
        await AddEmployeeRowAsync(finishedId, 1, "E001", "Alice", false, "boom", 100);

        var liveId = await CreateRunAsync(EngineType.PayrollEngine, EngineRunStatus.Running);
        await AddEmployeeRowAsync(liveId, 1, "E002", "Bob", true, "Successfull", 100);

        var otherEngineId = await CreateRunAsync(EngineType.TimeEngine, EngineRunStatus.Completed);

        await _reader.ClearAsync(EngineType.PayrollEngine);

        // What the operator actually sees: the cleared run is gone, the live one is still being reported.
        var status = await _reader.GetStatusAsync(EngineType.PayrollEngine);
        status.RunId.ShouldBe(liveId);

        var cleared = await _reader.GetEmployeesAsync(
            EngineType.PayrollEngine, new GetEngineRunEmployeesInput { RunId = finishedId });
        cleared.TotalCount.ShouldBe(0);

        await UsingDbContextAsync(async context =>
        {
            (await context.EngineRuns.FirstAsync(r => r.Id == finishedId)).IsDeleted.ShouldBeTrue();
            (await context.EngineRunEmployees.FirstAsync(r => r.EngineRunId == finishedId)).IsDeleted.ShouldBeTrue();

            (await context.EngineRuns.FirstAsync(r => r.Id == liveId)).IsDeleted.ShouldBeFalse();
            (await context.EngineRunEmployees.FirstAsync(r => r.EngineRunId == liveId)).IsDeleted.ShouldBeFalse();

            (await context.EngineRuns.FirstAsync(r => r.Id == otherEngineId)).IsDeleted.ShouldBeFalse(
                "Clearing one engine's history must not touch the other engine's.");
        });
    }

    #region << helpers >>

    private Task<long> CreateRunAsync(
        EngineType engineType,
        EngineRunStatus status = EngineRunStatus.Running,
        int total = 0,
        int succeeded = 0,
        int failed = 0,
        int skipped = 0,
        long totalProcessingMs = 0,
        DateTime? startedAt = null,
        DateTime? lastHeartbeatAt = null,
        DateTime? completedAt = null)
    {
        return UsingDbContextAsync(async context =>
        {
            var run = new EngineRun
            {
                TenantId = AbpSession.GetTenantId(),
                EngineType = engineType,
                RunKey = Guid.NewGuid().ToString("N"),
                Status = status,
                TotalEmployees = total,
                SucceededEmployees = succeeded,
                FailedEmployees = failed,
                SkippedEmployees = skipped,
                TotalProcessingMilliseconds = totalProcessingMs,
                StartedAt = startedAt ?? Clock.Now,
                LastHeartbeatAt = lastHeartbeatAt ?? Clock.Now,
                CompletedAt = completedAt
            };

            context.EngineRuns.Add(run);
            await context.SaveChangesAsync();
            return run.Id;
        });
    }

    private Task AddEmployeeRowAsync(
        long runId, int sequenceNo, string code, string name, bool succeeded, string message, int elapsedMs)
    {
        return UsingDbContextAsync(async context =>
        {
            context.EngineRunEmployees.Add(new EngineRunEmployee
            {
                TenantId = AbpSession.GetTenantId(),
                EngineRunId = runId,
                SequenceNo = sequenceNo,
                EmployeeCode = code,
                EmployeeName = name,
                Succeeded = succeeded,
                Message = message,
                StartedAt = Clock.Now,
                CompletedAt = Clock.Now,
                ElapsedMilliseconds = elapsedMs
            });

            await context.SaveChangesAsync();
        });
    }

    #endregion
}
