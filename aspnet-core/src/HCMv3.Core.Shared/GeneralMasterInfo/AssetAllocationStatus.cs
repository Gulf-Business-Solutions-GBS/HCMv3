namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.AssetAllocationStatus</c>.
/// In the legacy system this was a <c>[NonPersistent]</c> property computed from active
/// <c>EmployeeAsset</c> allocation records. Since <c>EmployeeAsset</c> (Payroll module) is not
/// ported in this wave, it is persisted here as a plain field instead of being computed.
/// </summary>
public enum AssetAllocationStatus
{
    Occupied = 0,
    Unoccupied = 1
}
