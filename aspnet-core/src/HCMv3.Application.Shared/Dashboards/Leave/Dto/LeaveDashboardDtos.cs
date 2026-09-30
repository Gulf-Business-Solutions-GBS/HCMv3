using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.Dashboards.Dto;
using HCMv3.Workflow;

namespace HCMv3.Dashboards.Leave.Dto;

/// <summary>How the leave trend is bucketed. Chosen by the server from the length of the date range.</summary>
public enum LeaveDashboardGranularity
{
    Month = 0,
    Day = 1
}

/// <summary>What an Excel export contains: the whole dashboard (one sheet per tile) or a single tile.</summary>
public enum LeaveDashboardExportPart
{
    All = 0,
    OnLeaveToday = 1,
    NotReturned = 2,
    StartingSoon = 3,
    ReturningSoon = 4,
    PendingApproval = 5,
    Trend = 6,
    ByOrgUnit = 7,
    ByType = 8,

    /// <summary>Every leave in the period, or on <see cref="ExportLeaveDashboardInput.Day"/> when that is set.</summary>
    LeavesInPeriod = 9
}

public class ExportLeaveDashboardInput : LeaveDashboardInput
{
    public LeaveDashboardExportPart Part { get; set; }

    /// <summary>Only with <see cref="LeaveDashboardExportPart.LeavesInPeriod"/>: the one day clicked on the daily trend.</summary>
    public DateTime? Day { get; set; }
}

/// <summary>Filter choices for the screen. Whose leave is shown is never a choice: the server decides it.</summary>
public class LeaveDashboardOptionsDto
{
    public List<LeaveDashboardAbsenceTypeOptionDto> AbsenceTypes { get; set; } = new();
}

public class LeaveDashboardAbsenceTypeOptionDto
{
    public int Id { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}

/// <summary>The filters every tile follows.</summary>
public class LeaveDashboardInput
{
    /// <summary>Start of the analysis period. Null means 1 January of the current year.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>End of the analysis period. Null means 31 December of the current year.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Restricts to employees currently assigned to this unit or any unit below it.</summary>
    public long? OrganizationUnitId { get; set; }

    public int? AbsenceTypeId { get; set; }
}

/// <summary>Input for the leave list behind a number or a chart bar.</summary>
public class GetLeaveDashboardLeavesInput : LeaveDashboardInput
{
    /// <summary>When set, only leave covering this one day (the daily trend's drill-down). Otherwise every
    /// leave overlapping the period.</summary>
    public DateTime? Day { get; set; }

    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }

    [Range(1, 1000)]
    public int MaxResultCount { get; set; } = 10;
}

public class LeaveDashboardDto
{
    /// <summary>Whose leave this is, decided by the server from the signed-in user.</summary>
    public DashboardScopeDto Scope { get; set; } = new();

    public DateTime AsOfDate { get; set; }

    /// <summary>The period actually used, after defaults and swapping a reversed range.</summary>
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public LeaveDashboardKpisDto Kpis { get; set; } = new();

    public List<LeaveDashboardLeaveRowDto> OnLeaveToday { get; set; } = new();

    public List<LeaveDashboardLeaveRowDto> NotReturned { get; set; } = new();

    public List<LeaveDashboardLeaveRowDto> StartingSoon { get; set; } = new();

    public List<LeaveDashboardLeaveRowDto> ReturningSoon { get; set; } = new();

    public List<LeaveDashboardLeaveRowDto> PendingApproval { get; set; } = new();

    public LeaveDashboardGranularity Granularity { get; set; }

    public List<LeaveDashboardTrendPointDto> Trend { get; set; } = new();

    public List<LeaveDashboardOrgUnitBucketDto> ByOrgUnit { get; set; } = new();

    public List<LeaveDashboardTypeBucketDto> ByType { get; set; } = new();

    /// <summary>Breadcrumb from the top down to the selected unit. Empty when no unit is selected.</summary>
    public List<LeaveDashboardOrgUnitOptionDto> OrgUnitPath { get; set; } = new();

    /// <summary>Units that hold at least one in-scope employee, plus their ancestors, in tree order.</summary>
    public List<LeaveDashboardOrgUnitOptionDto> OrgUnitOptions { get; set; } = new();
}

public class LeaveDashboardKpisDto
{
    public int EmployeesInScope { get; set; }

    public int OnLeaveToday { get; set; }

    public int NotReturned { get; set; }

    /// <summary>Employees whose leave starts in the next seven days (tomorrow onwards).</summary>
    public int StartingSoon { get; set; }

    /// <summary>Employees whose leave ends today or in the next six days, so they are due back within a week.</summary>
    public int ReturningSoon { get; set; }

    public int PendingApproval { get; set; }

    /// <summary>Leave days falling inside the period, pro-rated for leave that crosses its edges.</summary>
    public double LeaveDaysInPeriod { get; set; }
}

public class LeaveDashboardLeaveRowDto
{
    public int AbsenceId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeNameEn { get; set; }

    public string AbsenceTypeNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalLeaveDays { get; set; }

    /// <summary>For leave that has ended without a recorded return: whole days since the end date.</summary>
    public int DaysOverdue { get; set; }

    public RequestStatus? WorkflowStatus { get; set; }
}

public class LeaveDashboardTrendPointDto
{
    /// <summary>First day of the bucket: the 1st of the month, or the day itself.</summary>
    public DateTime PeriodStart { get; set; }

    public List<LeaveDashboardTrendValueDto> Values { get; set; } = new();
}

/// <summary>One leave type's share of a trend bucket. Monthly buckets carry leave days; daily buckets carry
/// the number of employees out that day.</summary>
public class LeaveDashboardTrendValueDto
{
    public int AbsenceTypeId { get; set; }

    public double Value { get; set; }
}

public class LeaveDashboardOrgUnitBucketDto
{
    /// <summary>Null for employees with no current organisation assignment.</summary>
    public long? OrganizationUnitId { get; set; }

    public string Name { get; set; }

    /// <summary>True when this bucket is the selected unit's own staff rather than a unit below it.</summary>
    public bool IsDirectStaff { get; set; }

    public bool HasChildren { get; set; }

    public int Employees { get; set; }

    public double LeaveDays { get; set; }
}

public class LeaveDashboardTypeBucketDto
{
    public int AbsenceTypeId { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int Employees { get; set; }

    public double LeaveDays { get; set; }
}

public class LeaveDashboardOrgUnitOptionDto
{
    public long Id { get; set; }

    public string Name { get; set; }

    public int Depth { get; set; }
}
