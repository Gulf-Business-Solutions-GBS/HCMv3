namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.DisciplinaryDeductionUnit</c>.
/// </summary>
public enum DisciplinaryDeductionUnit
{
    /// <summary>Added to deduction amount for days entered.</summary>
    Days = 0,

    /// <summary>Added to deduction amount for hours entered.</summary>
    Hours = 1,

    /// <summary>Added to direct deduction amount from salary.</summary>
    FixedAmount = 2,

    /// <summary>Added to percentage-wise deduction wage type amount which are having IncludeInDisiplinaryDed from salary.</summary>
    Percentage = 3
}
