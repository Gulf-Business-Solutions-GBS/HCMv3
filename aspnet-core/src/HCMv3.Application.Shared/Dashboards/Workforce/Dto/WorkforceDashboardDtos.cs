using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.Dashboards.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Dashboards.Workforce.Dto;

/// <summary>Age groups, as legacy ESS Workforce drew them, plus the under-21s legacy dropped.</summary>
public enum WorkforceAgeBand
{
    Under21 = 0,
    From21To30 = 1,
    From31To40 = 2,
    From41To50 = 3,
    Above50 = 4,

    /// <summary>No usable birth date on file.</summary>
    Unknown = 5
}

/// <summary>Completed years of service, in legacy ESS Workforce's groups.</summary>
public enum WorkforceServiceBand
{
    From0To4 = 0,
    From5To10 = 1,
    From11To15 = 2,
    From16To19 = 3,
    From20 = 4
}

/// <summary>Which employees a list or export shows.</summary>
public enum WorkforceEmployeeListKind
{
    /// <summary>Employed on the as-of date.</summary>
    Headcount = 0,

    /// <summary>Joined during the period.</summary>
    Joiners = 1,

    /// <summary>Left during the period (a recorded, unrevoked separation).</summary>
    Leavers = 2
}

/// <summary>What an Excel export contains: the whole dashboard (one sheet per tile) or a single tile.</summary>
public enum WorkforceDashboardExportPart
{
    All = 0,
    Headcount = 1,
    Joiners = 2,
    Leavers = 3,
    Trend = 4,
    Age = 5,
    Gender = 6,
    Nationality = 7,
    Service = 8,
    ByOrgUnit = 9,
    Qualification = 10,
    JobTitle = 11,
    Movements = 12,
    Documents = 13
}

/// <summary>
/// The filters every tile follows. Clicking a bar on the screen sets one of the attribute filters, so the whole
/// dashboard narrows to, say, female employees aged 31-40.
/// </summary>
public class WorkforceDashboardInput
{
    /// <summary>Start of the period for joiners, leavers and turnover. Null means 1 January this year.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>End of the period; headcount is taken on this date (or today, if it is in the future). Null means today.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Employees assigned to this unit or any unit below it.</summary>
    public long? OrganizationUnitId { get; set; }

    public Gender? Gender { get; set; }

    public int? NationalityId { get; set; }

    public WorkforceAgeBand? AgeBand { get; set; }

    public WorkforceServiceBand? ServiceBand { get; set; }

    /// <summary>0 means "no qualification recorded".</summary>
    public int? QualificationId { get; set; }

    /// <summary>0 means "no job title recorded".</summary>
    public int? JobTitleId { get; set; }
}

public class GetWorkforceEmployeesInput : WorkforceDashboardInput
{
    public WorkforceEmployeeListKind Kind { get; set; }

    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }

    [Range(1, 1000)]
    public int MaxResultCount { get; set; } = 10;
}

public class ExportWorkforceDashboardInput : WorkforceDashboardInput
{
    public WorkforceDashboardExportPart Part { get; set; }
}

public class WorkforceDashboardDto
{
    /// <summary>Whose employees these are, decided by the server from the signed-in user.</summary>
    public DashboardScopeDto Scope { get; set; } = new();

    /// <summary>The day headcount and the distributions are taken on: the end of the period, or today.</summary>
    public DateTime AsOfDate { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public WorkforceKpisDto Kpis { get; set; } = new();

    public List<WorkforceTrendPointDto> Trend { get; set; } = new();

    public List<WorkforceBandCountDto> AgeBands { get; set; } = new();

    public List<WorkforceBandCountDto> ServiceBands { get; set; } = new();

    public List<WorkforceGenderCountDto> Genders { get; set; } = new();

    public List<WorkforceNamedCountDto> Nationalities { get; set; } = new();

    public List<WorkforceNamedCountDto> Qualifications { get; set; } = new();

    public List<WorkforceNamedCountDto> JobTitles { get; set; } = new();

    public List<WorkforceOrgUnitBucketDto> ByOrgUnit { get; set; } = new();

    /// <summary>On hold, rejoined, separated, settled and awaiting settlement; every kind, in that order.</summary>
    public List<WorkforceMovementCountDto> Movements { get; set; } = new();

    /// <summary>Document types with documents expired or about to expire, most first.</summary>
    public List<WorkforceDocumentBucketDto> Documents { get; set; } = new();

    public List<DashboardOrgUnitOptionDto> OrgUnitPath { get; set; } = new();

    public List<DashboardOrgUnitOptionDto> OrgUnitOptions { get; set; } = new();
}

public class WorkforceKpisDto
{
    public int Headcount { get; set; }

    public int Male { get; set; }

    public int Female { get; set; }

    public int GenderNotSpecified { get; set; }

    /// <summary>Null when nobody has a usable birth date.</summary>
    public double? AverageAge { get; set; }

    public double? AverageServiceYears { get; set; }

    public int Nationalities { get; set; }

    public int Joiners { get; set; }

    public int Leavers { get; set; }

    public int Resignations { get; set; }

    public int Terminations { get; set; }

    /// <summary>Headcount on the first day of the period and on the as-of date, averaged.</summary>
    public double AverageHeadcount { get; set; }

    /// <summary>Leavers ÷ average headcount × 100. Null when the average headcount is 0.</summary>
    public double? TurnoverPercent { get; set; }
}

public class WorkforceTrendPointDto
{
    public DateTime MonthStart { get; set; }

    /// <summary>Employed on the last day of the month (or the as-of date, for the current month).</summary>
    public int Headcount { get; set; }

    public int Joiners { get; set; }

    public int Leavers { get; set; }

    public double? TurnoverPercent { get; set; }
}

public class WorkforceBandCountDto
{
    /// <summary>The <see cref="WorkforceAgeBand"/> or <see cref="WorkforceServiceBand"/> value.</summary>
    public int Band { get; set; }

    public int Count { get; set; }
}

public class WorkforceGenderCountDto
{
    public Gender Gender { get; set; }

    public int Count { get; set; }
}

/// <summary>One bar of a "top N" chart. <see cref="IsOthers"/> is the rest rolled together; Id 0 is "not recorded".</summary>
public class WorkforceNamedCountDto
{
    public int Id { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public bool IsOthers { get; set; }

    public int Count { get; set; }
}

public class WorkforceOrgUnitBucketDto
{
    /// <summary>Null for employees with no current organisation assignment.</summary>
    public long? OrganizationUnitId { get; set; }

    public string Name { get; set; }

    public bool IsDirectStaff { get; set; }

    public bool HasChildren { get; set; }

    public int Headcount { get; set; }

    public int Leavers { get; set; }

    public double? TurnoverPercent { get; set; }
}

public class WorkforceEmployeeRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public string JobTitleNameEn { get; set; }

    public string JobTitleNameAr { get; set; }

    public string NationalityNameEn { get; set; }

    public string NationalityNameAr { get; set; }

    public Gender Gender { get; set; }

    public int? Age { get; set; }

    public int ServiceYears { get; set; }

    public DateTime JoiningDate { get; set; }

    public DateTime? LastDayInService { get; set; }

    public SeparationType? SeparationType { get; set; }
}

/// <summary>The employee movements the Workforce Dashboard counts.</summary>
public enum WorkforceMovementKind
{
    /// <summary>Pay on hold on the as-of date.</summary>
    OnHold = 0,

    /// <summary>Rejoined during the period.</summary>
    Rejoined = 1,

    /// <summary>Last day in service during the period (a recorded, unrevoked separation).</summary>
    Separated = 2,

    /// <summary>Final settlement paid during the period.</summary>
    Settled = 3,

    /// <summary>Left by the as-of date with no final settlement recorded since their separation.</summary>
    AwaitingSettlement = 4
}

public enum WorkforceDocumentStatus
{
    Expired = 0,

    /// <summary>Expires within the document type's notification period.</summary>
    Expiring = 1
}

public class WorkforceMovementCountDto
{
    public WorkforceMovementKind Kind { get; set; }

    public int Count { get; set; }
}

public class WorkforceDocumentBucketDto
{
    public int DocumentTypeId { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>How many days ahead "expiring" looks for this type.</summary>
    public int NotificationDays { get; set; }

    public int Expired { get; set; }

    public int Expiring { get; set; }
}

public class GetWorkforceMovementsInput : WorkforceDashboardInput
{
    public WorkforceMovementKind Kind { get; set; }

    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }

    [Range(1, 1000)]
    public int MaxResultCount { get; set; } = 10;
}

public class GetWorkforceDocumentsInput : WorkforceDashboardInput
{
    /// <summary>Empty = every type.</summary>
    public int? DocumentTypeId { get; set; }

    /// <summary>Empty = expired and expiring.</summary>
    public WorkforceDocumentStatus? Status { get; set; }

    [Range(0, int.MaxValue)]
    public int SkipCount { get; set; }

    [Range(1, 1000)]
    public int MaxResultCount { get; set; } = 10;
}

public class WorkforceMovementRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public WorkforceMovementKind Kind { get; set; }

    /// <summary>On hold: from; rejoined: rejoin date; separated and awaiting: last day in service; settled: payment date.</summary>
    public DateTime Date { get; set; }

    /// <summary>On hold: until.</summary>
    public DateTime? EndDate { get; set; }
}

public class WorkforceDocumentRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public int DocumentTypeId { get; set; }

    public string DocumentTypeNameEn { get; set; }

    public string DocumentTypeNameAr { get; set; }

    public string DocumentNum { get; set; }

    public DateTime ExpiryDate { get; set; }

    /// <summary>Negative once expired.</summary>
    public int DaysLeft { get; set; }

    public WorkforceDocumentStatus Status { get; set; }
}
