namespace HCMv3.PayrollManagement;

/// <summary>
/// Deduction method for an <c>EmployeeSubGroupFund</c> pay-band step (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.FundDedType</c>).
/// </summary>
public enum FundDedType
{
    /// <summary>Percentage-wise deduction.</summary>
    Percentage = 0,

    /// <summary>Percentage-wise deduction, clamped between MinAmount and MaxAmount.</summary>
    PercentageWithinLimit = 1,

    /// <summary>Picked from EmployeeSubGroupFund.FixAmount.</summary>
    FixAmount = 2
}
