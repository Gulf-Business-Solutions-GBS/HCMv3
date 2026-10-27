namespace HCMv3.EngineRuns;

/// <summary>Shared tuning constants for live engine-run progress reporting.</summary>
public static class EngineRunConsts
{
    /// <summary>
    /// A run whose <c>LastHeartbeatAt</c> is older than this is considered dead. The engines heartbeat once
    /// per employee AND at least every <see cref="HeartbeatIntervalSeconds"/>, so this must comfortably
    /// exceed the slowest realistic single-employee processing time.
    /// </summary>
    public const int StaleHeartbeatSeconds = 300;

    /// <summary>
    /// Minimum wall-clock gap between two heartbeat writes. Prevents a fast run (thousands of employees a
    /// minute) from turning the heartbeat into a write storm; the per-employee progress row is written
    /// regardless, and carries its own timestamp.
    /// </summary>
    public const int HeartbeatIntervalSeconds = 10;

    /// <summary>Legacy truncates engine log messages to 1000 characters; progress rows match that.</summary>
    public const int MaxMessageLength = 1000;
}
