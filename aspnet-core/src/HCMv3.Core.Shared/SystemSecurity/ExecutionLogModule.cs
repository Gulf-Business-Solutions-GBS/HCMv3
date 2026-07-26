namespace HCMv3.SystemSecurity;

/// <summary>
/// Engine/module recorded by an <c>ExecutionLog</c> row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSecurity.Module</c>). Renamed from the legacy bare <c>Module</c> to
/// avoid a generic/collision-prone name.
/// </summary>
public enum ExecutionLogModule
{
    TimeEngine = 0,
    PayrollEngine = 1,
    BankFile = 2,
    GridReport = 3,
    PrintReport = 4,
    CostEngine = 5,
    ProjectEngine = 6
}
