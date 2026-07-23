namespace HCMv3.PayrollManagement;

/// <summary>
/// Duration type used to validate hour-based leave minutes for an <c>AbsenceType</c> (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.ResetFrequency</c>).
/// </summary>
public enum ResetFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Annually = 5
}
