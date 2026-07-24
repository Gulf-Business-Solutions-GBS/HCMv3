namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.LoanRecoveryMethod</c>
/// (declared on the legacy <c>EmployeeLoanRecovery</c> business object).
/// </summary>
public enum LoanRecoveryMethod
{
    /// <summary>Recovery via Payroll.</summary>
    RecoveryThroughPayroll = 0,

    /// <summary>Recovery via External Source.</summary>
    RecoveryExternal = 1,

    /// <summary>Recovery via EOS.</summary>
    RecoveryThroughEoS = 2,

    /// <summary>Recovery Exemption.</summary>
    RecoveryExemption = 3,

    /// <summary>Additional Recovery via Payroll.</summary>
    AddlRecoveryThroughPayroll = 4
}
