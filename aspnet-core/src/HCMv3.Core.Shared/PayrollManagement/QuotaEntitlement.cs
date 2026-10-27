namespace HCMv3.PayrollManagement;

/// <summary>
/// When a <c>TicketType</c>'s quota is accrued (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.QuotaEntitlement</c>, declared in
/// <c>AccrualPeriod.cs</c>).
/// </summary>
public enum QuotaEntitlement
{
    /// <summary>Accrual will be per day.</summary>
    Period = 0,

    /// <summary>Accrual will be immediate, after the first TE run.</summary>
    Immediate = 1
}
