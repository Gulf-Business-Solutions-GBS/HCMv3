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
    /// The Project Cost Engine (<c>ProjectCostEngineService</c>), one run at a time per tenant
    /// (run key <c>ProjectCostEngineService.RunKey</c>).
    /// </summary>
    ProjectCostEngine = 3
}
