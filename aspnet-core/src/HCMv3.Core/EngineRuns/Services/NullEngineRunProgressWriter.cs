using System.Threading.Tasks;

namespace HCMv3.EngineRuns.Services;

/// <summary>
/// Null Object default for <see cref="IEngineRunProgressWriter"/>.
///
/// <para>The engines take the writer as an OPTIONAL property injection defaulting to this, not as a
/// constructor dependency. Two reasons, in order of importance:</para>
/// <list type="number">
/// <item><description>Progress reporting is observability, not business logic. An engine that cannot report
/// progress must still calculate payroll. A constructor dependency makes the run impossible to construct
/// when the progress store is unavailable, which inverts that priority.</description></item>
/// <item><description>The engine unit tests construct the services with long positional argument lists and
/// assert calculation behaviour. Making them all declare a progress writer would add noise to tests that
/// have nothing to do with progress; those that DO care assign the property explicitly.</description></item>
/// </list>
/// </summary>
public sealed class NullEngineRunProgressWriter : IEngineRunProgressWriter
{
    public static readonly NullEngineRunProgressWriter Instance = new NullEngineRunProgressWriter();

    public Task<long> BeginRunAsync(EngineRunStartInfo info)
    {
        // 0 is the documented "progress unavailable" id; every other member no-ops on it.
        return Task.FromResult(0L);
    }

    public Task SetTotalEmployeesAsync(long engineRunId, int totalEmployees)
    {
        return Task.CompletedTask;
    }

    public Task RecordEmployeeAsync(EngineRunEmployeeProgress progress)
    {
        return Task.CompletedTask;
    }

    public Task RecordSkippedEmployeeAsync(long engineRunId)
    {
        return Task.CompletedTask;
    }

    public Task HeartbeatAsync(long engineRunId)
    {
        return Task.CompletedTask;
    }

    public Task<bool> IsStopRequestedAsync(long engineRunId)
    {
        return Task.FromResult(false);
    }

    public Task CompleteRunAsync(long engineRunId, EngineRunStatus status, int? executionLogId, string failureMessage)
    {
        return Task.CompletedTask;
    }
}
