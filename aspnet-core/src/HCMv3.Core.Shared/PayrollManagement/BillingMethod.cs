namespace HCMv3.PayrollManagement;

/// <summary>
/// Wage type proration billing method (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.BillingMethod</c>).
/// </summary>
public enum BillingMethod
{
    FixedAmountPerMonth = 0,
    FixedDaysPerMonth = 1,

    /// <summary>Actual calendar days.</summary>
    CalendarDaysPerMonth = 2,

    /// <summary>Calendar days minus weekend.</summary>
    WorkingDaysPerMonth = 3
}
