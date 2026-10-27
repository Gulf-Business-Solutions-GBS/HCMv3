using System;

namespace HCMv3.EngineRuns.Dto;

/// <summary>
/// Live progress summary for the most recent run of one engine. Polled by the operator screen, exactly as
/// <c>ProjectCostEngineStatusDto</c> is polled by <c>project-cost-engine.component.ts</c>.
///
/// <para><b>Contract note for the UI wave.</b> Every field below is either directly persisted or derived
/// from persisted counters -- nothing is inferred from wall-clock guesses except the two fields explicitly
/// named <c>Estimated*</c>. When <see cref="HasRun"/> is <c>false</c> the engine has never been run in this
/// tenant and every other field is at its default; render the "no runs yet" state rather than a zeroed
/// progress bar.</para>
/// </summary>
public class EngineRunStatusDto
{
    /// <summary><c>false</c> when this engine has no run rows at all. Render an empty state, not 0%.</summary>
    public bool HasRun { get; set; }

    public long RunId { get; set; }

    /// <summary>Opaque correlation id; pass it to <c>Get*EngineRunEmployees</c> to pin the grid to this run.</summary>
    public string RunKey { get; set; }

    public EngineType EngineType { get; set; }

    /// <summary>
    /// Raw persisted status. <see cref="EngineRunStatus.Running"/> is a claim, not a fact -- always
    /// cross-check <see cref="IsStale"/> before showing a spinner.
    /// </summary>
    public EngineRunStatus Status { get; set; }

    /// <summary>Convenience for the UI: <see cref="Status"/> is Running AND the run is not stale.</summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// <c>true</c> when the run still claims to be Running but has not heart-beat for
    /// <see cref="StaleThresholdSeconds"/>. This is how a crashed or killed run is surfaced: no support
    /// ticket, no run stuck "in progress" forever. The row is flipped to
    /// <see cref="EngineRunStatus.Abandoned"/> permanently either by the next run of the same engine or by
    /// an explicit <c>AbandonStale*EngineRuns</c> call; until then this flag ages it out visually.
    /// </summary>
    public bool IsStale { get; set; }

    public int StaleThresholdSeconds { get; set; }

    /// <summary>Seconds since <see cref="LastHeartbeatAt"/>. Drives the staleness countdown in the UI.</summary>
    public double SecondsSinceHeartbeat { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime LastHeartbeatAt { get; set; }

    /// <summary><c>null</c> while the run is in flight.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Wall-clock seconds from <see cref="StartedAt"/> to <see cref="CompletedAt"/>, or to "now" while the
    /// run is in flight. This is the ACTUAL elapsed time once finished -- not a sum of per-employee timings,
    /// which excludes setup and finalisation.
    /// </summary>
    public double ElapsedSeconds { get; set; }

    /// <summary>
    /// Employees selected for the run. <c>0</c> during the brief window between the run starting and
    /// employee selection completing -- treat 0 as "counting..." rather than "nothing to do".
    /// </summary>
    public int TotalEmployees { get; set; }

    /// <summary><see cref="SucceededEmployees"/> + <see cref="FailedEmployees"/>.</summary>
    public int ProcessedEmployees { get; set; }

    public int SucceededEmployees { get; set; }

    public int FailedEmployees { get; set; }

    /// <summary>
    /// Employees the engine chose not to process at all (they get no grid row). Counted separately so that
    /// <see cref="RemainingEmployees"/> converges to zero.
    /// </summary>
    public int SkippedEmployees { get; set; }

    /// <summary><see cref="TotalEmployees"/> - <see cref="ProcessedEmployees"/> - <see cref="SkippedEmployees"/>, floored at 0.</summary>
    public int RemainingEmployees { get; set; }

    /// <summary>0-100, rounded to one decimal. <c>0</c> while <see cref="TotalEmployees"/> is still 0.</summary>
    public double ProgressPercent { get; set; }

    /// <summary>
    /// MEASURED mean of the per-employee stopwatch timings of the employees completed so far -- a plain
    /// average, no smoothing. <c>null</c> until at least one employee has completed.
    /// </summary>
    public double? AverageSecondsPerEmployee { get; set; }

    /// <summary>
    /// <b>An estimate.</b> <see cref="RemainingEmployees"/> x <see cref="AverageSecondsPerEmployee"/>.
    /// <c>null</c> until at least one employee has completed, and <c>null</c> once the run has finished. It
    /// assumes the remaining employees cost the same as those already done, which is often false (employees
    /// with more absences, more retro periods, or more wage types take longer). Present it as approximate.
    /// </summary>
    public double? EstimatedSecondsRemaining { get; set; }

    /// <summary><b>An estimate.</b> Now + <see cref="EstimatedSecondsRemaining"/>. Same caveats.</summary>
    public DateTime? EstimatedCompletionTime { get; set; }

    /// <summary>Whether an operator has pressed Stop. The engine checks this between employees.</summary>
    public bool StopRequested { get; set; }

    /// <summary>"Code - Name" of the most recently completed employee (legacy <c>lblCurrntEmp</c>).</summary>
    public string CurrentEmployee { get; set; }

    public string StartedByUserName { get; set; }

    /// <summary>Pipe-separated run parameters, identical to <c>ExecutionLog.Parameters</c>.</summary>
    public string Parameters { get; set; }

    /// <summary>Run-level (not per-employee) failure text. Populated for Failed and Abandoned runs.</summary>
    public string FailureMessage { get; set; }

    /// <summary>Correlates to <c>ExecutionLog</c>; only populated once the run has finalised.</summary>
    public int? ExecutionLogId { get; set; }
}
