namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.AdjustmentType</c>
/// (declared on the legacy <c>EmployeeAbsAdjustment</c> business object). Named generically (not
/// "AbsAdjustmentType") because the legacy enum is also reused by <c>EmployeeTktAdjustment</c>
/// (Wave 3, not yet ported).
/// </summary>
public enum AdjustmentType
{
    /// <summary>To pay the employee for the leave quota instead of providing leave.</summary>
    Encashment = 0,

    /// <summary>To adjust leave quota for the employee.</summary>
    Adjust = 1,

    /// <summary>To adjust fractional leave quota for the employee.</summary>
    Fractional = 2,

    /// <summary>To adjust a difference in salary register closing balance without impacting leave provision.</summary>
    NoImpactOnQuota = 3
}
