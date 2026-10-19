using System;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The calculations legacy <c>EmployeeAttendance</c> ran on save (legacy
/// <c>HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeAttendance.cs</c>):
/// <c>TotalAttendDays</c> for the Period and Hours units (<c>UpdateAttendanceQuota</c>, lines 348-381, via
/// <c>GetHoursToAttendanceDay</c> 465-489 and <c>GetPlannedminutes</c> 490-573), and the leave-quota accrual
/// cascade into the attendance type's linked <see cref="EmployeeAbsenceQuota"/> (lines 383-436, delete 309-343,
/// <c>AddEmployeeAbsQuota</c> 652-733).
///
/// <para>
/// Duty resolution is NOT re-ported: it is <see cref="IEmployeeAbsenceCalculationService.GetEmployeeDutyAsync"/>,
/// which is the same legacy roster / substitute / weekly duty-period lookup (legacy attendance lines 574-649
/// are a copy of the absence ones).
/// </para>
/// </summary>
public interface IEmployeeAttendanceCalculationService : IDomainService
{
    /// <summary>
    /// Legacy <c>UpdateAttendanceQuota</c> "Calculate TotalAttendDays" region (lines 348-381). Sets
    /// <see cref="EmployeeAttendance.TotalAttendDays"/> (and, for a Period with both times, also
    /// <see cref="EmployeeAttendance.AttendanceHours"/>) on the row:
    /// <list type="bullet">
    /// <item><description>Period with StartTime AND EndTime: hours = EndTime - StartTime rounded to 3; days =
    /// hours over the planned duty minutes, clamped to [0, 1] (lines 351-361).</description></item>
    /// <item><description>Period without both times: EndDate - StartDate + 1 (line 365).</description></item>
    /// <item><description>Hours: AttendanceHours over the planned duty minutes, floored at 0, NOT capped
    /// (lines 372-380).</description></item>
    /// <item><description>Day: left untouched -- legacy kept the entered value (lines 368-371).</description></item>
    /// </list>
    /// </summary>
    Task ApplyTotalAttendDaysAsync(EmployeeAttendance attendance);

    /// <summary>Legacy <c>GetHoursToAttendanceDay</c> (lines 465-489).</summary>
    Task<double> GetHoursToAttendanceDayAsync(int employeeId, DateTime attendanceDayDate, double attendanceHours);

    /// <summary>
    /// Legacy <c>GetPlannedminutes</c> (lines 490-573): the duty's shift minutes less its break, floored at 0.
    /// Refuses a weekly-off / rest-day duty unless Time Settings allow attendance on one, and refuses a day
    /// with no duty.
    /// </summary>
    Task<double> GetPlannedMinutesAsync(int employeeId, DateTime attendanceDayDate);

    /// <summary>
    /// Legacy "Update Previous Quota" (lines 383-409) and <c>OnDeleting</c> (309-343): takes the previously
    /// saved <see cref="AttendanceQuotaSnapshot.TotalAttendDays"/> off the quota of the previous type's linked
    /// leave type. See the implementation for the legacy oddities kept.
    /// </summary>
    Task ReleaseAttendanceQuotaAsync(AttendanceQuotaSnapshot previous);

    /// <summary>Legacy "Update if QuotaAccrual Found" (lines 411-436).</summary>
    Task AccrueAttendanceQuotaAsync(EmployeeAttendance attendance);
}

/// <summary>
/// Legacy's non-persistent <c>prevEmployee</c>/<c>prevAttendanceType</c>/<c>prevTotalAttendDays</c>
/// (EmployeeAttendance.cs lines 132-153), captured in <c>OnLoaded</c> (155-163).
/// </summary>
public sealed class AttendanceQuotaSnapshot
{
    public int EmployeeId { get; init; }

    public int AttendanceTypeId { get; init; }

    public double TotalAttendDays { get; init; }

    public static AttendanceQuotaSnapshot Of(EmployeeAttendance attendance) => new()
    {
        EmployeeId = attendance.EmployeeId,
        AttendanceTypeId = attendance.AttendanceTypeId,
        TotalAttendDays = attendance.TotalAttendDays
    };
}
