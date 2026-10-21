namespace HCMv3.EngineRuns;

/// <summary>
/// Which of an engine's runs a screen follows. Without a scope the reader follows the engine's latest run, which
/// is what the Payroll Engine screen does. The Time Engine screen follows the latest run of the payroll area it
/// has selected, and never the time-engine runs a payroll run made for itself (see
/// <c>TimeEngineRunKeys</c>).
/// </summary>
public class EngineRunScope
{
    /// <summary>Only runs with exactly this <see cref="EngineRun.RunKey"/>. Null = any key.</summary>
    public string RunKey { get; set; }

    /// <summary>Leave out runs whose key starts with this. Null = leave none out.</summary>
    public string ExcludedRunKeyPrefix { get; set; }
}
