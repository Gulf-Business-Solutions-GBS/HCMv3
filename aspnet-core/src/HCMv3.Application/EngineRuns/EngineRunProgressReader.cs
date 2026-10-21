using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Timing;
using HCMv3.EngineRuns.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.EngineRuns;

/// <summary>
/// Shared read side for live engine-run progress. Both <c>TimeEngineAppService</c> and
/// <c>PayrollEngineAppService</c> delegate here so the two engines expose an IDENTICAL contract -- one DTO,
/// one set of semantics, one UI component.
///
/// <para><b>This class must only ever touch <c>EngineRun</c> and <c>EngineRunEmployee</c>.</b> Those two
/// tables are written exclusively on the progress writer's short-lived independent transactions, so reading
/// them never waits on the engine's hour-long transaction. The moment this reader joins to <c>Employee</c>,
/// <c>TimeLog</c>, <c>PayrollLog</c> or <c>ExecutionLog</c>, the status endpoint can block on locks the
/// running engine holds and the progress screen hangs -- which is worse than showing no progress. That is
/// why employee code and name are DENORMALISED onto <c>EngineRunEmployee</c> rather than joined.</para>
///
/// <para>A plain injectable (not an app service): it performs no authorisation and is not remotely
/// exposed. Its callers authorise.</para>
/// </summary>
public class EngineRunProgressReader : ITransientDependency
{
    private readonly IRepository<EngineRun, long> _engineRunRepository;
    private readonly IRepository<EngineRunEmployee, long> _engineRunEmployeeRepository;

    public EngineRunProgressReader(
        IRepository<EngineRun, long> engineRunRepository,
        IRepository<EngineRunEmployee, long> engineRunEmployeeRepository)
    {
        _engineRunRepository = engineRunRepository;
        _engineRunEmployeeRepository = engineRunEmployeeRepository;
    }

    /// <summary>Latest run of the given engine for the current tenant, projected into the polling DTO.</summary>
    [UnitOfWork]
    public virtual Task<EngineRunStatusDto> GetStatusAsync(EngineType engineType)
    {
        return GetStatusAsync(engineType, null);
    }

    /// <summary>
    /// Latest run of the given engine within <paramref name="scope"/> (null = every run of the engine), projected
    /// into the polling DTO.
    /// </summary>
    [UnitOfWork]
    public virtual async Task<EngineRunStatusDto> GetStatusAsync(EngineType engineType, EngineRunScope scope)
    {
        var run = await InScope(_engineRunRepository.GetAll().Where(r => r.EngineType == engineType), scope)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        if (run == null)
        {
            return new EngineRunStatusDto
            {
                HasRun = false,
                EngineType = engineType,
                StaleThresholdSeconds = EngineRunConsts.StaleHeartbeatSeconds
            };
        }

        var now = Clock.Now;
        var processed = run.SucceededEmployees + run.FailedEmployees;
        var remaining = run.TotalEmployees - processed - run.SkippedEmployees;
        if (remaining < 0)
        {
            remaining = 0;
        }

        var secondsSinceHeartbeat = (now - run.LastHeartbeatAt).TotalSeconds;
        var isStale = run.Status == EngineRunStatus.Running
                      && secondsSinceHeartbeat > EngineRunConsts.StaleHeartbeatSeconds;

        var dto = new EngineRunStatusDto
        {
            HasRun = true,
            RunId = run.Id,
            RunKey = run.RunKey,
            EngineType = run.EngineType,
            Status = run.Status,
            IsRunning = run.Status == EngineRunStatus.Running && !isStale,
            IsStale = isStale,
            StaleThresholdSeconds = EngineRunConsts.StaleHeartbeatSeconds,
            SecondsSinceHeartbeat = Math.Round(secondsSinceHeartbeat, 1),
            StartedAt = run.StartedAt,
            LastHeartbeatAt = run.LastHeartbeatAt,
            CompletedAt = run.CompletedAt,
            TotalEmployees = run.TotalEmployees,
            ProcessedEmployees = processed,
            SucceededEmployees = run.SucceededEmployees,
            FailedEmployees = run.FailedEmployees,
            SkippedEmployees = run.SkippedEmployees,
            RemainingEmployees = remaining,
            StopRequested = run.StopRequested,
            StartedByUserName = run.StartedByUserName,
            Parameters = run.Parameters,
            FailureMessage = run.FailureMessage,
            ExecutionLogId = run.ExecutionLogId
        };

        // Actual elapsed once finished; live wall-clock while in flight. For an abandoned run the heartbeat
        // is the last moment the process was demonstrably alive, so it is the honest end point.
        var endedAt = run.CompletedAt ?? (isStale ? run.LastHeartbeatAt : now);
        dto.ElapsedSeconds = Math.Round(Math.Max(0, (endedAt - run.StartedAt).TotalSeconds), 1);

        if (run.TotalEmployees > 0)
        {
            dto.ProgressPercent = Math.Round(
                100d * (processed + run.SkippedEmployees) / run.TotalEmployees, 1);
        }

        // MEASURED average -- a plain mean over completed employees, no smoothing. Null (not zero) until
        // there is something to average, so the UI can show "estimating..." instead of a bogus "0s".
        if (processed > 0)
        {
            var averageSeconds = run.TotalProcessingMilliseconds / 1000d / processed;
            dto.AverageSecondsPerEmployee = Math.Round(averageSeconds, 3);

            // An ESTIMATE, and only meaningful while the run is actually going somewhere.
            if (dto.IsRunning && remaining > 0)
            {
                var estimate = averageSeconds * remaining;
                dto.EstimatedSecondsRemaining = Math.Round(estimate, 1);
                dto.EstimatedCompletionTime = now.AddSeconds(estimate);
            }
        }

        // Legacy lblCurrntEmp. Read from the denormalised columns -- never joined to Employee, see remarks.
        var latest = await _engineRunEmployeeRepository.GetAll()
            .Where(r => r.EngineRunId == run.Id)
            .OrderByDescending(r => r.Id)
            .Select(r => new { r.EmployeeCode, r.EmployeeName })
            .FirstOrDefaultAsync();

        if (latest != null)
        {
            dto.CurrentEmployee = string.IsNullOrWhiteSpace(latest.EmployeeName)
                ? latest.EmployeeCode
                : latest.EmployeeCode + " - " + latest.EmployeeName;
        }

        return dto;
    }

    /// <summary>
    /// The per-employee grid for one run. <c>input.RunId == null</c> follows the latest run of the engine,
    /// which is what a live screen needs (it polls before it knows the run id).
    /// </summary>
    [UnitOfWork]
    public virtual Task<PagedResultDto<EngineRunEmployeeDto>> GetEmployeesAsync(
        EngineType engineType, GetEngineRunEmployeesInput input)
    {
        return GetEmployeesAsync(engineType, input, null);
    }

    /// <summary>
    /// The per-employee grid for one run. <c>input.RunId == null</c> follows the latest run of the engine within
    /// <paramref name="scope"/>; an explicit run id is honoured as long as it is this engine's.
    /// </summary>
    [UnitOfWork]
    public virtual async Task<PagedResultDto<EngineRunEmployeeDto>> GetEmployeesAsync(
        EngineType engineType, GetEngineRunEmployeesInput input, EngineRunScope scope)
    {
        var runId = input.RunId;

        if (runId == null)
        {
            runId = await InScope(_engineRunRepository.GetAll().Where(r => r.EngineType == engineType), scope)
                .OrderByDescending(r => r.Id)
                .Select(r => (long?)r.Id)
                .FirstOrDefaultAsync();
        }
        else
        {
            // Never let one engine's screen page through the other engine's run.
            var belongsToEngine = await _engineRunRepository.GetAll()
                .AnyAsync(r => r.Id == runId.Value && r.EngineType == engineType);

            if (!belongsToEngine)
            {
                runId = null;
            }
        }

        if (runId == null)
        {
            return new PagedResultDto<EngineRunEmployeeDto>(0, new List<EngineRunEmployeeDto>());
        }

        var query = _engineRunEmployeeRepository.GetAll()
            .Where(r => r.EngineRunId == runId.Value)
            .WhereIf(input.OnlyFailed == true, r => !r.Succeeded)
            .WhereIf(input.OnlyFailed == false, r => r.Succeeded)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                r => r.EmployeeCode.Contains(input.Filter)
                     || r.EmployeeName.Contains(input.Filter)
                     || r.Message.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "SequenceNo" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var items = rows.Select(r => new EngineRunEmployeeDto
        {
            Id = r.Id,
            SequenceNo = r.SequenceNo,
            EmployeeId = r.EmployeeId,
            EmployeeCode = r.EmployeeCode,
            EmployeeName = r.EmployeeName,
            Succeeded = r.Succeeded,
            Message = r.Message,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            ElapsedMilliseconds = r.ElapsedMilliseconds,
            ElapsedSeconds = Math.Round(r.ElapsedMilliseconds / 1000d, 3)
        }).ToList();

        return new PagedResultDto<EngineRunEmployeeDto>(totalCount, items);
    }

    /// <summary>
    /// Sets the Stop flag on the engine's current run. The engine re-reads it between employees on its own
    /// connection, so the request lands mid-run.
    ///
    /// <para>Deliberately does NOT write <c>TimeLog.StopFlag</c>/<c>PayrollLog.StopFlag</c> as the legacy
    /// screens did: those tables are being written by the engine's open transaction, so an UPDATE from this
    /// connection risks blocking behind it. The engines still honour the legacy flags as well.</para>
    /// </summary>
    /// <returns><c>true</c> if a running run was flagged.</returns>
    [UnitOfWork]
    public virtual Task<bool> RequestStopAsync(EngineType engineType)
    {
        return RequestStopAsync(engineType, null);
    }

    /// <summary>
    /// Stops the latest running run of the engine within <paramref name="scope"/> -- so stopping one payroll
    /// area's Time Engine run leaves another area's alone.
    /// </summary>
    /// <returns><c>true</c> if a running run was flagged.</returns>
    [UnitOfWork]
    public virtual async Task<bool> RequestStopAsync(EngineType engineType, EngineRunScope scope)
    {
        var run = await InScope(
                _engineRunRepository.GetAll().Where(r => r.EngineType == engineType && r.Status == EngineRunStatus.Running),
                scope)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        if (run == null)
        {
            return false;
        }

        run.StopRequested = true;
        await _engineRunRepository.UpdateAsync(run);
        return true;
    }

    /// <summary>
    /// Deletes this engine's progress history (legacy's "Clear Status" button). The run currently in flight
    /// is preserved -- clearing it would orphan a live engine's counters.
    /// </summary>
    [UnitOfWork]
    public virtual async Task ClearAsync(EngineType engineType)
    {
        var runIds = await _engineRunRepository.GetAll()
            .Where(r => r.EngineType == engineType && r.Status != EngineRunStatus.Running)
            .Select(r => r.Id)
            .ToListAsync();

        if (!runIds.Any())
        {
            return;
        }

        await _engineRunEmployeeRepository.DeleteAsync(r => runIds.Contains(r.EngineRunId));
        await _engineRunRepository.DeleteAsync(r => runIds.Contains(r.Id));
    }

    /// <summary>
    /// Permanently flips runs that stopped heart-beating to <see cref="EngineRunStatus.Abandoned"/>. This is
    /// the explicit form of the crash handling; the same sweep also runs automatically whenever a new run of
    /// the engine starts, so an operator never has to call this to unblock themselves.
    /// </summary>
    /// <returns>How many runs were aged out.</returns>
    [UnitOfWork]
    public virtual async Task<int> AbandonStaleRunsAsync(EngineType engineType)
    {
        var cutoff = Clock.Now.AddSeconds(-EngineRunConsts.StaleHeartbeatSeconds);

        var stale = await _engineRunRepository.GetAll()
            .Where(r => r.EngineType == engineType
                        && r.Status == EngineRunStatus.Running
                        && r.LastHeartbeatAt < cutoff)
            .ToListAsync();

        foreach (var run in stale)
        {
            run.Status = EngineRunStatus.Abandoned;
            run.CompletedAt = run.LastHeartbeatAt;
            run.FailureMessage = "Run stopped reporting progress and was never finalised (process killed, crashed, or host recycled).";
            await _engineRunRepository.UpdateAsync(run);
        }

        return stale.Count;
    }

    private static IQueryable<EngineRun> InScope(IQueryable<EngineRun> query, EngineRunScope scope)
    {
        if (scope == null)
        {
            return query;
        }

        var runKey = scope.RunKey;
        var excludedPrefix = scope.ExcludedRunKeyPrefix;

        return query
            .WhereIf(!runKey.IsNullOrWhiteSpace(), r => r.RunKey == runKey)
            .WhereIf(!excludedPrefix.IsNullOrWhiteSpace(), r => r.RunKey == null || !r.RunKey.StartsWith(excludedPrefix));
    }
}
