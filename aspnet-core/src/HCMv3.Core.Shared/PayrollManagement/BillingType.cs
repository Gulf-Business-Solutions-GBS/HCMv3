namespace HCMv3.PayrollManagement;

/// <summary>
/// Wage type billing classification (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.BillingType</c>). <see cref="TimeType"/> and
/// <see cref="Provision"/> are reserved for internal/system use — <c>WageTypesAppService</c> blocks creating
/// a new <c>WageType</c> with either value (legacy <c>PerformValidation</c> parity).
/// </summary>
public enum BillingType
{
    Earning = 0,
    Deduction = 1,
    TimeType = 2,
    Provision = 3
}
