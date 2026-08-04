namespace HCMv3.EngineRuns;

/// <summary>
/// Identifies which long-running calculation engine an <c>EngineRun</c> progress row belongs to.
///
/// <para>Explicit numeric values: these are persisted, so re-ordering the members must never renumber
/// existing rows.</para>
/// </summary>
public enum EngineType
{
    TimeEngine = 1,

    PayrollEngine = 2,

    /// <summary>
    /// Reserved. The Project Cost Engine still reports progress through its own legacy
    /// <c>ProjectLog</c>-derived <c>ProjectCostEngineStatusDto</c> and is NOT instrumented here.
    /// </summary>
    ProjectCostEngine = 3
}
