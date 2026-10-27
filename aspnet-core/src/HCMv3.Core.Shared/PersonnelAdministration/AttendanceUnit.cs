namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.AttendanceUnit</c>
/// (declared on the legacy <c>EmployeeAttendance</c> business object).
/// </summary>
public enum AttendanceUnit
{
    /// <summary>StartDate/EndDate are required (period-based attendance).</summary>
    Period = 0,

    /// <summary>Only TotalAttendDays is used for direct day-count entry.</summary>
    Day = 1,

    /// <summary>StartDate/AttendanceHours are required; TotalAttendDays is derived from duty on StartDate.</summary>
    Hours = 2
}
