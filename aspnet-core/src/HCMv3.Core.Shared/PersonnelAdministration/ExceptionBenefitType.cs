namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Ported generically from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDependantException</c>'s nested enum.
/// Kept generic (not Dependant-specific) in naming, since it is also reused by
/// <c>EmployeeBenefitException</c> in Wave 4.
/// </summary>
public enum ExceptionBenefitType
{
    Ticket = 0,
    HealthReimbursment = 1
}
