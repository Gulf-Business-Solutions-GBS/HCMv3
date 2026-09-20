using System.Threading.Tasks;

namespace HCMv3.EngineRuns.Services;

/// <summary>
/// Publishes live progress for a long-running engine run.
///
/// <para><b>Every method commits in its own unit of work</b>
/// (<c>IUnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew)</c>). That is the entire point of this
/// abstraction: both engines run inside ONE outer unit of work that completes only at the very end, so a
/// progress row written into that unit of work becomes visible only when the run finishes -- which is
/// precisely when progress has stopped being useful. Writes here are on a separate connection and
/// transaction, so a concurrent reader sees them while the run is still open.</para>
///
/// <para><b>No method may ever throw.</b> Progress reporting is observability, not business logic; a failure
/// to write a progress row must never fail (or slow to a crawl) a payroll run. Implementations swallow and
/// log. Callers therefore do not need try/catch.</para>
///
/// <para><b>Never read or write the engines' own tables from here.</b> Doing so would make these
/// independent transactions contend with the main run's locks -- see <see cref="EngineRun"/> remarks.</para>
/// </summary>
public interface IEngineRunProgressWriter
{
    /// <summary>
    /// Opens a run row in <see cref="EngineRunStatus.Running"/> and returns its id. Also ages out any
    /// previous run of the same engine type that is still marked Running but has stopped heart-beating --
    /// see <see cref="EngineRunStatus.Abandoned"/>.
    /// </summary>
    /// <returns>The run id, or <c>0</c> if the row could not be written (progress is then a no-op).</returns>
    Task<long> BeginRunAsync(EngineRunStartInfo info);

    /// <summary>Publishes the expected employee count once employee selection has resolved it.</summary>
    Task SetTotalEmployeesAsync(long engineRunId, int totalEmployees);

    /// <summary>
    /// Appends one finished employee and updates the run's counters and heartbeat -- both in the same
    /// independent transaction, so the grid and the summary can never disagree.
    /// </summary>
    Task RecordEmployeeAsync(EngineRunEmployeeProgress progress);

    /// <summary>
    /// Records an employee the engine chose not to process (legacy's decrementing "continue" branches). No
    /// grid row; the run's remaining count shrinks so the ETA still converges.
    /// </summary>
    Task RecordSkippedEmployeeAsync(long engineRunId);

    /// <summary>
    /// Refreshes <see cref="EngineRun.LastHeartbeatAt"/>, throttled to
    /// <c>EngineRunConsts.HeartbeatIntervalSeconds</c>. Call it from inside long per-employee work so a slow
    /// employee is not mistaken for a dead process.
    /// </summary>
    Task HeartbeatAsync(long engineRunId);

    /// <summary>
    /// Reads the operator's Stop request on an independent connection. Returns <c>false</c> on any error --
    /// a progress-store hiccup must not stop a run.
    /// </summary>
    Task<bool> IsStopRequestedAsync(long engineRunId);

    /// <summary>Closes the run. Safe to call more than once; safe to call with <c>engineRunId == 0</c>.</summary>
    Task CompleteRunAsync(long engineRunId, EngineRunStatus status, int? executionLogId, string failureMessage);

    /// <summary>
    /// The run still going for this engine and <paramref name="runKey"/> in the current tenant, or <c>null</c>
    /// when nothing is running. Stale rows are aged out to <see cref="EngineRunStatus.Abandoned"/> first, so a
    /// run whose process was killed does NOT block the next one for ever -- which is what usually makes a lock
    /// like this worse than the problem it solves.
    ///
    /// <para><b>What it is for.</b> Two payroll runs for the SAME payroll area write the same
    /// <c>SalaryRegister</c> rows for the same employees, and the advance-recovery upsert ACCUMULATES -- so a
    /// concurrent pair can double a recovery. Legacy neither prevented this nor coped with it: its Run button
    /// truncated the whole <c>PayrollLog</c> table and queued another thread, so a second run destroyed the
    /// first one's progress log and the two runs' counters were added together. There is nothing to port; this
    /// is the guard legacy lacked.</para>
    ///
    /// <para>Runs with DIFFERENT keys are unaffected -- different payroll areas may run at the same time.</para>
    ///
    /// <para>Returns <c>null</c> on any error, deliberately: a progress-store hiccup must not be able to block
    /// payroll. The guard fails OPEN.</para>
    /// </summary>
    Task<ActiveEngineRun> GetActiveRunAsync(EngineType engineType, string runKey);
}
