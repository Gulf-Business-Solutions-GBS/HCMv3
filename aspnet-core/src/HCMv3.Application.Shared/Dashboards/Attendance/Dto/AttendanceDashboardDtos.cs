using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.Dashboards.Dto;
using HCMv3.Workflow;

namespace HCMv3.Dashboards.Attendance.Dto;

/// <summary>How the trend is bucketed. Chosen by the server from the length of the period.</summary>
public enum AttendanceDashboardGranularity
{
    Month = 0,
    Day = 1
}

/// <summary>Which attendance days a list shows.</summary>
public enum AttendanceExceptionKind
{
    Absent = 0,
    Late = 1,
    EarlyOut = 2,
    MissingPunch = 3,

    /// <summary>Every exception of any kind (with <see cref="GetAttendanceExceptionsInput.Day"/>: on that day).</summary>
    Any = 4
}

/// <summary>What an Excel export contains: the whole dashboard (one sheet per tile) or a single tile.</summary>
public enum AttendanceDashboardExportPart
{
    All = 0,
    Trend = 1,
    ByOrgUnit = 2,
    TopAbsent = 3,
    TopLate = 4,
    TopEarly = 5,
    Weekday = 6,
    Exceptions = 7,
    PunchRequests = 8
}

/// <summary>The filters every tile follows.</summary>
public class AttendanceDashboardInput
{
    /// <summary>Null means the first day of the current month.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Null means today.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Employees assigned to this unit or any unit below it.</summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>One employee, set by clicking a name on a "most" chart.</summary>
    public int? EmployeeId { get; set; }
}

public class GetAttendanceExceptionsInput : AttendanceDashboardInput
{
    public AttendanceExceptionKind Kind { get; set; }

    /// <summary>Only this day (the daily chart's drill-down).</summary>
    public DateTime? Day { get; set; }

    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }

    [Range(1, 1000)]
    public int MaxResultCount { get; set; } = 10;
}

public class GetAttendancePunchRequestsInput : AttendanceDashboardInput
{
    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }

    [Range(1, 1000)]
    public int MaxResultCount { get; set; } = 10;
}

public class ExportAttendanceDashboardInput : AttendanceDashboardInput
{
    public AttendanceDashboardExportPart Part { get; set; }

    /// <summary>With <see cref="AttendanceDashboardExportPart.Exceptions"/>: which list.</summary>
    public AttendanceExceptionKind Kind { get; set; } = AttendanceExceptionKind.Any;

    public DateTime? Day { get; set; }
}

public class AttendanceDashboardDto
{
    public DashboardScopeDto Scope { get; set; } = new();

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    /// <summary>The name of the employee the dashboard is filtered to, for the chip.</summary>
    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public AttendanceKpisDto Kpis { get; set; } = new();

    public AttendanceDashboardGranularity Granularity { get; set; }

    public List<AttendanceTrendPointDto> Trend { get; set; } = new();

    public List<AttendanceOrgUnitBucketDto> ByOrgUnit { get; set; } = new();

    public List<AttendanceTopEmployeeDto> TopAbsent { get; set; } = new();

    public List<AttendanceTopEmployeeDto> TopLate { get; set; } = new();

    public List<AttendanceTopEmployeeDto> TopEarly { get; set; } = new();

    public List<AttendanceWeekdayDto> Weekdays { get; set; } = new();

    public List<DashboardOrgUnitOptionDto> OrgUnitPath { get; set; } = new();

    public List<DashboardOrgUnitOptionDto> OrgUnitOptions { get; set; } = new();
}

public class AttendanceKpisDto
{
    /// <summary>Days an employee was scheduled to work: a planned working day, not on leave, not a holiday or day off.</summary>
    public int WorkingDays { get; set; }

    public int PresentDays { get; set; }

    public int AbsentDays { get; set; }

    /// <summary>Present days ÷ working days × 100. Null when there are no working days.</summary>
    public double? AttendanceRatePercent { get; set; }

    public int LateDays { get; set; }

    public double LateHours { get; set; }

    public int EarlyDays { get; set; }

    public double EarlyHours { get; set; }

    public int MissingPunchDays { get; set; }

    public double PlannedHours { get; set; }

    public double ActualHours { get; set; }

    public int PunchRequests { get; set; }

    public int PunchRequestsPending { get; set; }
}

public class AttendanceTrendPointDto
{
    public DateTime PeriodStart { get; set; }

    public int WorkingDays { get; set; }

    public int AbsentDays { get; set; }

    public int LateDays { get; set; }

    public int EarlyDays { get; set; }

    public double? AttendanceRatePercent { get; set; }
}

public class AttendanceOrgUnitBucketDto
{
    public long? OrganizationUnitId { get; set; }

    public string Name { get; set; }

    public bool IsDirectStaff { get; set; }

    public bool HasChildren { get; set; }

    public int WorkingDays { get; set; }

    public int AbsentDays { get; set; }

    public int LateDays { get; set; }

    public int EarlyDays { get; set; }

    public double? AttendanceRatePercent { get; set; }
}

/// <summary>One bar of a "most absent / most late / most early" chart.</summary>
public class AttendanceTopEmployeeDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    /// <summary>Days (absent) or hours (late, early), per chart.</summary>
    public double Value { get; set; }

    /// <summary>How many days the value is spread over.</summary>
    public int Days { get; set; }
}

public class AttendanceWeekdayDto
{
    /// <summary>0 = Sunday ... 6 = Saturday.</summary>
    public int DayOfWeek { get; set; }

    public int LateDays { get; set; }

    public int AbsentDays { get; set; }
}

public class AttendanceExceptionRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public DateTime DayDate { get; set; }

    public DateTime? PlannedIn { get; set; }

    public DateTime? PlannedOut { get; set; }

    public DateTime? ActualIn { get; set; }

    public DateTime? ActualOut { get; set; }

    public bool IsAbsent { get; set; }

    public double LateMinutes { get; set; }

    public double EarlyMinutes { get; set; }

    public bool IsMissingIn { get; set; }

    public bool IsMissingOut { get; set; }
}

public class AttendancePunchRequestRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public DateTime PunchDate { get; set; }

    public DateTime PunchTime { get; set; }

    public string PunchTypeNameEn { get; set; }

    public string PunchTypeNameAr { get; set; }

    public RequestStatus? Status { get; set; }
}
