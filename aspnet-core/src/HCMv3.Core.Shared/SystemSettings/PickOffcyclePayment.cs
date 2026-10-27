namespace HCMv3.SystemSettings;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.SysSettings.ManagePayrollSettings</c>
/// (nested enum promoted to a top-level enum here).
/// </summary>
public enum PickOffcyclePayment
{
    CurrentDate = 0, // Pick off cycle payment records up to current date
    CurrentMonth = 1 // Pick off cycle payment records up to current month end date
}
