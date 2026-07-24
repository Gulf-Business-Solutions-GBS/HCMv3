namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.AssetDeductionMode</c>
/// (declared on the legacy <c>EmployeeAsset</c> business object).
/// </summary>
public enum AssetDeductionMode
{
    /// <summary>Don't do any deduction for this Asset bill.</summary>
    None = 0,

    /// <summary>Do Full deduction for this Asset bill.</summary>
    Full = 1,

    /// <summary>Do deduction beyond Allowance in Salary profile for this Asset.</summary>
    AboveAllowance = 2
}
