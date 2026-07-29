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
    ProjectEngine = 6,

    /// <summary>
    /// ADDED BY THIS PORT (no legacy member). The legacy bulk import/export screen
    /// (<c>uscBulkImportExport</c>) wrote no <c>ExecutionLog</c> row at all -- it recomputed its counters
    /// from <c>BulkUploadLog</c> on every UI timer tick. This port follows the established HCMv3 convention
    /// of one <c>ExecutionLog</c> summary row per engine run, which needs a module value.
    /// </summary>
    BulkImportExport = 7
}
