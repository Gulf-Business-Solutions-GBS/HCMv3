namespace HCMv3.SystemSecurity;

/// <summary>
/// Action recorded by an <c>ExecutionLog</c> row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSecurity.Action</c>). Renamed from the legacy bare <c>Action</c> to
/// avoid colliding with <c>System.Action</c>.
/// </summary>
public enum ExecutionLogAction
{
    RUN = 0,
    GENERATE = 1,
    VIEW = 2,
    EXPORT = 3,
    PRINT = 4
}
