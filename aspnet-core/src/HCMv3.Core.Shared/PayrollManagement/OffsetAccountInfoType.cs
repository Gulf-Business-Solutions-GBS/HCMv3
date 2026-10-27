namespace HCMv3.PayrollManagement;

/// <summary>
/// Offset account resolution mode for <c>WageAccGroupDetail</c>/<c>WageAccount</c> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.OffsetAccountInfoType</c>).
/// </summary>
public enum OffsetAccountInfoType
{
    Fixed = 0,
    EmployeeCode = 1
}
