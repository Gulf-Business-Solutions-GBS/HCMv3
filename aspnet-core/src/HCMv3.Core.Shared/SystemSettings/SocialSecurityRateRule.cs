namespace HCMv3.SystemSettings;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.SysSettings.ManagePayrollSettings</c>
/// (nested enum promoted to a top-level enum here).
/// </summary>
public enum SocialSecurityRateRule
{
    WageSalary = 0,     // From Wage Detail in Employee Profile screen
    ContractSalary = 1  // From Contract Salary in Employee Sponsor screen
}
