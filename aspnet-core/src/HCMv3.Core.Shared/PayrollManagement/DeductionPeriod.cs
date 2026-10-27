namespace HCMv3.PayrollManagement;

/// <summary>
/// When a <c>TicketType</c>'s minimum-compensation deduction applies (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.DeductionPeriod</c>, declared inside
/// <c>TicketType.cs</c>).
/// </summary>
public enum DeductionPeriod
{
    AfterEntitlement = 0,
    SameAsEntitlement = 1
}
