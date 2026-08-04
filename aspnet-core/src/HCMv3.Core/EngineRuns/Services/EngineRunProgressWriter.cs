using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Timing;

namespace HCMv3.EngineRuns.Services;

/// <inheritdoc cref="IEngineRunProgressWriter"/>
/// <remarks>
/// <para><b>The one thing to understand about this class:</b> every public method body is wrapped in
/// <c>using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))</c> followed by
/// <c>await uow.CompleteAsync()</c>. ABP gives a <c>RequiresNew</c> unit of work its own transaction
/// strategy instance, so it opens a fresh DbConnection and a fresh transaction rather than enlisting in the
/// caller's. The write therefore COMMITS immediately, while the engine's outer transaction -- which may not
/// commit for another hour -- is still open. Without this, progress rows would surface only at the end of
/// the run and the whole feature would be a lie.</para>
///
/// <para>The tenant filter still applies inside the new unit of work because <see cref="IAbpSession"/> is
/// ambient; <c>TenantId</c> is nonetheless set explicitly on insert rather than relying on ABP's
/// auto-assignment, per this repo's convention.</para>
///
/// <para><b>Failure policy:</b> every method catches everything and logs a warning. A progress-store failure
/// must never fail a payroll run. <see cref="BeginRunAsync"/> returning <c>0</c> is the documented
/// "progress unavailable" signal, and every other method no-ops on a zero id, so an engine can call these
/// unconditionally without null checks.</para>
/// </remarks>
public class EngineRunProgressWriter : HCMv3DomainServiceBase, IEngineRunProgressWriter
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<EngineRun, long> _engineRunRepository;
    private readonly IRepository<EngineRunEmployee, long> _engineRunEmployeeRepository;
    private readonly IAbpSession _abpSession;

    /// <summary>
    /// Throttle state for <see cref="HeartbeatAsync"/>. Instance state is safe: ABP domain services are
    /// transient, so one writer instance serves one run (the same reason
    /// <c>PayrollEngineService.SkipWithoutDecrement</c> can be an instance field).
    /// </summary>
    private DateTime _lastHeartbeatWrittenAt = DateTime.MinValue;

    public EngineRunProgressWriter(
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<EngineRun, long> engineRunRepository,
        IRepository<EngineRunEmployee, long> engineRunEmployeeRepository,
        IAbpSession abpSession)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _engineRunRepository = engineRunRepository;
        _engineRunEmployeeRepository = engineRunEmployeeRepository;
        _abpSession = abpSession;
    }

    public async Task<long> BeginRunAsync(EngineRunStartInfo info)
    {
        if (info == null)
        {
            return 0;
        }

        try
        {
            using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
            {
                var now = Clock.Now;

                // Self-healing crash handling: whatever killed the previous run (process kill, machine
                // shutdown, host recycle) could not have run any cleanup, so the only moment we are
                // guaranteed to be alive and looking at that row is now. See EngineRunStatus.Abandoned.
                await AbandonStaleRunsInCurrentUowAsync(info.EngineType, now);

                var run = new EngineRun
                {
                    TenantId = _abpSession.GetTenantId(),
                    EngineType = info.EngineType,
                    RunKey = string.IsNullOrWhiteSpace(info.RunKey) ? Guid.NewGuid().ToString("N") : info.RunKey,
                    StartedByUserName = info.StartedByUserName,
                    Parameters = info.Parameters,
                    StartedAt = now,
                    LastHeartbeatAt = now,
                    Status = EngineRunStatus.Running
                };

                var id = await _engineRunRepository.InsertAndGetIdAsync(run);
                await uow.CompleteAsync();

                _lastHeartbeatWrittenAt = now;
                return id;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("EngineRunProgressWriter.BeginRunAsync failed; the run will proceed without progress reporting.", ex);
            return 0;
        }
    }

    public async Task SetTotalEmployeesAsync(long engineRunId, int totalEmployees)
    {
        if (engineRunId <= 0)
        {
            return;
        }

        try
        {
            using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
            {
                var run = await _engineRunRepository.FirstOrDefaultAsync(engineRunId);
                if (run != null)
                {
                    run.TotalEmployees = totalEmployees;
                    run.LastHeartbeatAt = Clock.Now;
                    await _engineRunRepository.UpdateAsync(run);
                }

                await uow.CompleteAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("EngineRunProgressWriter.SetTotalEmployeesAsync failed.", ex);
        }
    }

    public async Task RecordEmployeeAsync(EngineRunEmployeeProgress progress)
    {
        if (progress == null || progress.EngineRunId <= 0)
        {
            return;
        }

        try
        {
            using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
            {
                var now = Clock.Now;

                await _engineRunEmployeeRepository.InsertAsync(new EngineRunEmployee
                {
                    TenantId = _abpSession.GetTenantId(),
                    EngineRunId = progress.EngineRunId,
                    SequenceNo = progress.SequenceNo,
                    EmployeeId = progress.EmployeeId,
                    EmployeeCode = progress.EmployeeCode,
                    EmployeeName = progress.EmployeeName,
                    Succeeded = progress.Succeeded,
                    Message = Truncate(progress.Message),
                    StartedAt = progress.StartedAt,
                    CompletedAt = progress.CompletedAt,
                    ElapsedMilliseconds = progress.ElapsedMilliseconds
                });

                // Counters are updated in the SAME independent transaction as the grid row, so the summary
                // and the grid can never be observed out of step with each other.
                var run = await _engineRunRepository.FirstOrDefaultAsync(progress.EngineRunId);
                if (run != null)
                {
                    if (progress.Succeeded)
                    {
                        run.SucceededEmployees = run.SucceededEmployees + 1;
                    }
                    else
                    {
                        run.FailedEmployees = run.FailedEmployees + 1;
                    }

                    run.TotalProcessingMilliseconds = run.TotalProcessingMilliseconds + progress.ElapsedMilliseconds;
                    run.LastHeartbeatAt = now;
                    await _engineRunRepository.UpdateAsync(run);
                }

                await uow.CompleteAsync();

                _lastHeartbeatWrittenAt = now;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("EngineRunProgressWriter.RecordEmployeeAsync failed.", ex);
        }
    }

    public async Task RecordSkippedEmployeeAsync(long engineRunId)
    {
        if (engineRunId <= 0)
        {
            return;
        }

        try
        {
            using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
            {
                var now = Clock.Now;

                var run = await _engineRunRepository.FirstOrDefaultAsync(engineRunId);
                if (run != null)
                {
                    run.SkippedEmployees = run.SkippedEmployees + 1;
                    run.LastHeartbeatAt = now;
                    await _engineRunRepository.UpdateAsync(run);
                }

                await uow.CompleteAsync();

                _lastHeartbeatWrittenAt = now;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("EngineRunProgressWriter.RecordSkippedEmployeeAsync failed.", ex);
        }
    }

    public async Task HeartbeatAsync(long engineRunId)
    {
        if (engineRunId <= 0)
        {
            return;
        }

        var now = Clock.Now;
        if ((now - _lastHeartbeatWrittenAt).TotalSeconds < EngineRunConsts.HeartbeatIntervalSeconds)
        {
            return;
        }

        try
        {
            using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
            {
                var run = await _engineRunRepository.FirstOrDefaultAsync(engineRunId);
                if (run != null)
                {
                    run.LastHeartbeatAt = now;
                    await _engineRunRepository.UpdateAsync(run);
                }

                await uow.CompleteAsync();
            }

            _lastHeartbeatWrittenAt = now;
        }
        catch (Exception ex)
        {
            Logger.Warn("EngineRunProgressWriter.HeartbeatAsync failed.", ex);
        }
    }

    public async Task<bool> IsStopRequestedAsync(long engineRunId)
    {
        if (engineRunId <= 0)
        {
            return false;
        }

        try
        {
            using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
            {
                // The read must happen on the independent connection too: reading through the engine's own
                // open transaction would return the snapshot that existed when the run started, and the Stop
                // button would appear to do nothing.
                var stopRequested = _engineRunRepository.GetAll()
                    .Where(r => r.Id == engineRunId)
                    .Select(r => r.StopRequested)
                    .FirstOrDefault();

                await uow.CompleteAsync();
                return stopRequested;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("EngineRunProgressWriter.IsStopRequestedAsync failed; treating as 'not stopped'.", ex);
            return false;
        }
    }

    public async Task CompleteRunAsync(long engineRunId, EngineRunStatus status, int? executionLogId, string failureMessage)
    {
        if (engineRunId <= 0)
        {
            return;
        }

        try
        {
            using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
            {
                var now = Clock.Now;

                var run = await _engineRunRepository.FirstOrDefaultAsync(engineRunId);
                if (run != null)
                {
                    run.Status = status;
                    run.CompletedAt = now;
                    run.LastHeartbeatAt = now;
                    run.ExecutionLogId = executionLogId;
                    run.FailureMessage = Truncate(failureMessage);
                    await _engineRunRepository.UpdateAsync(run);
                }

                await uow.CompleteAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("EngineRunProgressWriter.CompleteRunAsync failed.", ex);
        }
    }

    /// <summary>
    /// Marks previous runs of the same engine that are still <see cref="EngineRunStatus.Running"/> but whose
    /// heartbeat has aged past <c>EngineRunConsts.StaleHeartbeatSeconds</c> as
    /// <see cref="EngineRunStatus.Abandoned"/>. Must be called from INSIDE an already-open unit of work.
    /// </summary>
    private async Task AbandonStaleRunsInCurrentUowAsync(EngineType engineType, DateTime now)
    {
        var cutoff = now.AddSeconds(-EngineRunConsts.StaleHeartbeatSeconds);

        var stale = _engineRunRepository.GetAll()
            .Where(r => r.EngineType == engineType
                        && r.Status == EngineRunStatus.Running
                        && r.LastHeartbeatAt < cutoff)
            .ToList();

        foreach (var run in stale)
        {
            run.Status = EngineRunStatus.Abandoned;
            run.CompletedAt = run.LastHeartbeatAt;
            run.FailureMessage = "Run stopped reporting progress and was never finalised (process killed, crashed, or host recycled).";
            await _engineRunRepository.UpdateAsync(run);
        }
    }

    private static string Truncate(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        return message.Length > EngineRunConsts.MaxMessageLength
            ? message.Substring(0, EngineRunConsts.MaxMessageLength)
            : message;
    }
}
