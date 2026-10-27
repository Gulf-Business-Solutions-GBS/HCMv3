namespace HCMv3.BenefitsManagement;

/// <summary>
/// Ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.BenefitsManagement.EmployeeInsuranceDetail</c> (nested
/// <c>InstallmentType</c> enum). Describes how an insurance installment is/was recovered.
/// </summary>
public enum InstallmentType
{
    ThroughPayroll = 0,
    External = 1,
    EndOfService = 2,
    Exemption = 3
}
