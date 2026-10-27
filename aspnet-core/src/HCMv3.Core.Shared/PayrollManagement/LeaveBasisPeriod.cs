namespace HCMv3.PayrollManagement;

/// <summary>
/// Basis on which leave amount is calculated for an <c>AbsenceType</c> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.LeaveBasisPeriod</c>).
/// </summary>
public enum LeaveBasisPeriod
{
    CalendarDays = 0,
    FixedDays = 1
}
