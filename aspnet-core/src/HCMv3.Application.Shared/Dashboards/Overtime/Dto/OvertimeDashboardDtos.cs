using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.Dashboards.Dto;
using HCMv3.Workflow;

namespace HCMv3.Dashboards.Overtime.Dto;

/// <summary>Which overtime entries a list shows.</summary>
public enum OvertimeEntryListKind
{
    /// <summary>Approved entries of the period.</summary>
    Approved = 0,

    /// <summary>Entries submitted for approval and not decided yet, whatever their month.</summary>
    Pending = 1
}

/// <summary>What an Excel export contains: the whole dashboard (one sheet per tile) or a single tile.</summary>
public enum OvertimeDashboardExportPart
{
    All = 0,
    Trend = 1,
    CostTrend = 2,
    ByType = 3,
    ByOrgUnit = 4,
    TopEmployees = 5,
    Entries = 6
}

/// <summary>The filters every tile follows.</summary>
public class OvertimeDashboardInput
{
    /// <summary>Null means 1 January this year.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Null means today.</summary>
    public DateTime? ToDate { get; set; }

    public long? OrganizationUnitId { get; set; }

    /// <summary>One overtime type, set by clicking a slice.</summary>
    public int? OvertimeTypeId { get; set; }

    /// <summary>One employee, set by clicking a name on the "most overtime" chart.</summary>
    public int? EmployeeId { get; set; }
}

public class GetOvertimeEntriesInput : OvertimeDashboardInput
{
    public OvertimeEntryListKind Kind { get; set; }

    /// <summary>Only this month (the monthly chart's drill-down): any day in it.</summary>
    public DateTime? Month { get; set; }

    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }

    [Range(1, 1000)]
    public int MaxResultCount { get; set; } = 10;
}

public class ExportOvertimeDashboardInput : OvertimeDashboardInput
{
    public OvertimeDashboardExportPart Part { get; set; }

    public OvertimeEntryListKind Kind { get; set; }

    public DateTime? Month { get; set; }
}

public class OvertimeDashboardDto
{
    public DashboardScopeDto Scope { get; set; } = new();

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public OvertimeKpisDto Kpis { get; set; } = new();

    /// <summary>Every overtime type, for the colours and the chips (not only those with hours).</summary>
    public List<OvertimeTypeOptionDto> Types { get; set; } = new();

    public List<OvertimeTrendPointDto> Trend { get; set; } = new();

    public List<OvertimeTypeBucketDto> ByType { get; set; } = new();

    public List<OvertimeOrgUnitBucketDto> ByOrgUnit { get; set; } = new();

    public List<OvertimeTopEmployeeDto> TopEmployees { get; set; } = new();

    public List<DashboardOrgUnitOptionDto> OrgUnitPath { get; set; } = new();

    public List<DashboardOrgUnitOptionDto> OrgUnitOptions { get; set; } = new();
}

public class OvertimeKpisDto
{
    public double ApprovedHours { get; set; }

    /// <summary>Hours claimed on the same entries, before approval.</summary>
    public double ClaimedHours { get; set; }

    /// <summary>Approved ÷ claimed × 100. Null when nothing was claimed.</summary>
    public double? ApprovedOfClaimedPercent { get; set; }

    public int Employees { get; set; }

    /// <summary>Approved hours ÷ employees with overtime. Null when nobody had any.</summary>
    public double? AverageHoursPerEmployee { get; set; }

    /// <summary>What payroll paid under the overtime wage types for the months of the period.</summary>
    public double Cost { get; set; }

    public int PendingEntries { get; set; }

    public double PendingHours { get; set; }
}

public class OvertimeTypeOptionDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}

public class OvertimeTrendPointDto
{
    public DateTime MonthStart { get; set; }

    public List<OvertimeTrendValueDto> Hours { get; set; } = new();

    public double Cost { get; set; }
}

public class OvertimeTrendValueDto
{
    public int OvertimeTypeId { get; set; }

    public double Hours { get; set; }
}

public class OvertimeTypeBucketDto
{
    public int OvertimeTypeId { get; set; }

    public double Hours { get; set; }

    public int Employees { get; set; }

    public double Cost { get; set; }
}

public class OvertimeOrgUnitBucketDto
{
    public long? OrganizationUnitId { get; set; }

    public string Name { get; set; }

    public bool IsDirectStaff { get; set; }

    public bool HasChildren { get; set; }

    public double Hours { get; set; }

    public int Employees { get; set; }

    public double Cost { get; set; }
}

public class OvertimeTopEmployeeDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public double Hours { get; set; }

    public double Cost { get; set; }
}

public class OvertimeEntryRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public DateTime EffectiveDate { get; set; }

    public int OvertimeTypeId { get; set; }

    public double ClaimedHours { get; set; }

    public double ActualHours { get; set; }

    public double ApprovedHours { get; set; }

    public RequestStatus? Status { get; set; }
}
