namespace HCMv3.SystemSecurity;

/// <summary>
/// Outcome recorded by an <c>ExecutionLog</c> row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSecurity.ExecutionStatus</c>). Renamed from the legacy bare
/// <c>ExecutionStatus</c> to avoid colliding with the entity's own <c>ExecutionStatus</c> property name.
/// Legacy spelling <c>INPRECESS</c> is preserved verbatim (not corrected to "INPROCESS") to stay a faithful
/// data-value port; only the storage representation matters, not the label spelling.
/// </summary>
public enum ExecutionLogStatus
{
    INPRECESS = 0,
    SUCCESS = 1,
    FAILED = 2,
    PARTIAL = 3,
    ABORTED = 4
}
