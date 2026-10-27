namespace HCMv3.PayrollManagement;

/// <summary>
/// Start month used when <c>AbsenceType.AccrualPeriod</c> (or <c>AttendanceType.AccrualPeriod</c>) is
/// <see cref="AccrualPeriod.Custom"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.CustomStartMonth</c>).
/// </summary>
public enum CustomStartMonth
{
    None = 0,
    January = 1,
    February = 2,
    March = 3,
    April = 4,
    May = 5,
    June = 6,
    July = 7,
    August = 8,
    September = 9,
    October = 10,
    November = 11,
    December = 12
}
