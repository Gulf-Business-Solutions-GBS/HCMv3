namespace HCMv3.PayrollManagement;

/// <summary>
/// Quota-accrual period, shared by <c>AbsenceType</c> and <c>AttendanceType</c> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AccrualPeriod</c>). Note: the legacy enum member
/// <c>PerEmployement</c> is a misspelling — it is ported here as the correctly spelled <see cref="PerEmployment"/>.
/// </summary>
public enum AccrualPeriod
{
    NoAccrual = 0,
    CalendarYear = 1,
    FiscalYear = 2,
    WorkYear = 3,
    BiAnnual = 4,
    PerEmployment = 5,
    Custom = 6
}
