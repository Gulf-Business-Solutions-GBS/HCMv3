namespace HCMv3.EngineRuns;

/// <summary>
/// Lifecycle of a single engine run, as seen by the operator screen.
///
/// <para>Deliberately NOT reusing <c>ExecutionLogStatus</c>: that enum is written once at the END of a run
/// (and its INPRECESS value is invisible mid-run because the whole run shares one unit of work). This enum
/// is maintained transactionally-independently while the run is in flight, and adds
/// <see cref="Abandoned"/>, which <c>ExecutionLogStatus</c> has no concept of.</para>
///
/// <para>Explicit numeric values: persisted.</para>
/// </summary>
public enum EngineRunStatus
{
    /// <summary>The run is (or claims to be) in flight. Cross-check the heartbeat before believing it.</summary>
    Running = 0,

    /// <summary>Every selected employee was processed, all successfully.</summary>
    Completed = 1,

    /// <summary>The run finished but at least one employee failed.</summary>
    CompletedWithErrors = 2,

    /// <summary>The run itself threw (not a per-employee failure) and stopped early.</summary>
    Failed = 3,

    /// <summary>The run was stopped by an operator (stop flag) before processing every employee.</summary>
    Aborted = 4,

    /// <summary>
    /// The run stopped heart-beating and was never finalised -- the process was killed, crashed, or the host
    /// recycled. Set lazily, when a later run of the same engine starts, or by
    /// <c>AbandonStaleEngineRuns</c>. See <c>EngineRunConsts.StaleHeartbeatSeconds</c>.
    /// </summary>
    Abandoned = 5
}
