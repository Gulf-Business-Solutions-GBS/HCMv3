namespace HCMv3.PayrollManagement;

/// <summary>
/// Pay frequency for an <c>EmployeeSubGroupPayment</c> / <c>EmployeeBasicPay</c> record (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.PayInterval</c>, declared inside
/// <c>EmployeeBasicPay.cs</c>). Note: the legacy enum member <c>Yealy</c> is a misspelling — it is ported
/// here as the correctly spelled <see cref="Yearly"/>.
/// </summary>
public enum PayInterval
{
    Monthly = 0,
    Daily = 1,
    Hourly = 2,
    EndOfService = 3,
    Yearly = 4
}
