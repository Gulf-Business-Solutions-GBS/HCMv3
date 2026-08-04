using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;

namespace HCMv3.Tests.EngineRuns;

/// <summary>
/// Records what an engine publishes, so engine tests can assert the progress CONTRACT (begin, total,
/// one row per processed employee with a measured duration, skips counted, run closed with the right
/// status) without a database.
///
/// <para>Transaction visibility -- the hard part -- is proved separately and against a real store by
/// <c>EngineRunProgressWriter_Tests</c>. This fake deliberately proves nothing about that.</para>
/// </summary>
public class RecordingEngineRunProgressWriter : IEngineRunProgressWriter
{
    public const long TestRunId = 777;

    public List<EngineRunStartInfo> StartedRuns { get; } = new List<EngineRunStartInfo>();

    public List<int> TotalsPublished { get; } = new List<int>();

    public List<EngineRunEmployeeProgress> Employees { get; } = new List<EngineRunEmployeeProgress>();

    public int SkippedCount { get; private set; }

    public int HeartbeatCount { get; private set; }

    public bool Completed { get; private set; }

    public EngineRunStatus CompletedStatus { get; private set; }

    public int? CompletedExecutionLogId { get; private set; }

    public string CompletedFailureMessage { get; private set; }

    /// <summary>Set to make the next stop check return true, simulating the operator's Stop button.</summary>
    public bool StopRequested { get; set; }

    /// <summary>Hook for subclasses that need to observe WHEN a run is opened relative to other work.</summary>
    protected virtual void OnBegin()
    {
    }

    public Task<long> BeginRunAsync(EngineRunStartInfo info)
    {
        OnBegin();
        StartedRuns.Add(info);
        return Task.FromResult(TestRunId);
    }

    public Task SetTotalEmployeesAsync(long engineRunId, int totalEmployees)
    {
        TotalsPublished.Add(totalEmployees);
        return Task.CompletedTask;
    }

    public Task RecordEmployeeAsync(EngineRunEmployeeProgress progress)
    {
        Employees.Add(progress);
        return Task.CompletedTask;
    }

    public Task RecordSkippedEmployeeAsync(long engineRunId)
    {
        SkippedCount++;
        return Task.CompletedTask;
    }

    public Task HeartbeatAsync(long engineRunId)
    {
        HeartbeatCount++;
        return Task.CompletedTask;
    }

    public Task<bool> IsStopRequestedAsync(long engineRunId)
    {
        return Task.FromResult(StopRequested);
    }

    public Task CompleteRunAsync(long engineRunId, EngineRunStatus status, int? executionLogId, string failureMessage)
    {
        Completed = true;
        CompletedStatus = status;
        CompletedExecutionLogId = executionLogId;
        CompletedFailureMessage = failureMessage;
        return Task.CompletedTask;
    }
}
