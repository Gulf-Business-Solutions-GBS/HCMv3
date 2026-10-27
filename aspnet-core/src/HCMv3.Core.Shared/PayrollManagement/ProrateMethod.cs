namespace HCMv3.PayrollManagement;

/// <summary>
/// Wage type prorate method (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.ProrateMethod</c>).
/// </summary>
public enum ProrateMethod
{
    WorkingDays = 0,
    WorkingDays_REST = 1,
    ActiveDays = 2
}
