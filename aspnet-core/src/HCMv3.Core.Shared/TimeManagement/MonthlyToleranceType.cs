namespace HCMv3.TimeManagement;

/// <summary>
/// How monthly late-in / early-out tolerance is calculated for a <see cref="HCMv3.TimeManagement.TimePolicy"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.MonthlyToleranceType</c>).
/// </summary>
public enum MonthlyToleranceType
{
    NotSelected = 0,
    CalcMonthlyToleranceOnActual = 1,
    CalcMonthlyTolearnceOnDed = 2
}
