using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.Dashboards.Dto;
using HCMv3.Workflow;

namespace HCMv3.Dashboards.Workflow.Dto;

/// <summary>Which requests a clicked figure lists.</summary>
public enum WorkflowRequestListKind
{
    /// <summary>Every request raised in the period.</summary>
    All = 0,

    /// <summary>Requests with one status.</summary>
    Status = 1,

    /// <summary>Requests raised in one month.</summary>
    Month = 2,

    /// <summary>Open requests past their due date.</summary>
    Overdue = 3,

    /// <summary>Open requests waiting with one approver.</summary>
    Approver = 4,

    /// <summary>Open requests waiting a number of days in one band.</summary>
    WaitingBand = 5
}

public enum WorkflowDashboardExportPart
{
    All = 0,
    Trend = 1,
    ByType = 2,
    ByOrgUnit = 3,
    Waiting = 4,
    WaitingBands = 5,
    Requests = 6
}

public class WorkflowDashboardInput
{
    /// <summary>First day requests were raised on. Default: 1 January of this year.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Last day requests were raised on. Default: today.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Only requests of employees in this unit or below it.</summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>Only this request type (<c>WorkflowTransactionType.Code</c>).</summary>
    public string RequestType { get; set; }
}

public class GetWorkflowRequestsInput : WorkflowDashboardInput, IPagedResultRequest
{
    public WorkflowRequestListKind Kind { get; set; }

    public RequestStatus? Status { get; set; }

    /// <summary>For <see cref="WorkflowRequestListKind.Month"/>: any day of the month.</summary>
    public DateTime? Month { get; set; }

    /// <summary>For <see cref="WorkflowRequestListKind.Approver"/>: the approver's employee id.</summary>
    public int? ApproverId { get; set; }

    /// <summary>For <see cref="WorkflowRequestListKind.WaitingBand"/>: the band's index.</summary>
    public int? Band { get; set; }

    public int SkipCount { get; set; }

    public int MaxResultCount { get; set; } = 10;
}

public class ExportWorkflowDashboardInput : WorkflowDashboardInput
{
    public WorkflowDashboardExportPart Part { get; set; }

    public WorkflowRequestListKind Kind { get; set; }

    public RequestStatus? Status { get; set; }

    public DateTime? Month { get; set; }

    public int? ApproverId { get; set; }

    public int? Band { get; set; }
}

public class WorkflowDashboardDto
{
    public DashboardScopeDto Scope { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public WorkflowKpisDto Kpis { get; set; }

    /// <summary>Every request type raised in the period, for names and the filter.</summary>
    public List<WorkflowRequestTypeOptionDto> Types { get; set; } = new();

    public List<WorkflowTrendPointDto> Trend { get; set; } = new();

    public List<WorkflowTypeBucketDto> ByType { get; set; } = new();

    public List<WorkflowOrgUnitBucketDto> ByOrgUnit { get; set; } = new();

    public List<WorkflowApproverBucketDto> Waiting { get; set; } = new();

    public List<WorkflowWaitingBandDto> WaitingBands { get; set; } = new();

    public List<DashboardOrgUnitOptionDto> OrgUnitPath { get; set; } = new();

    public List<DashboardOrgUnitOptionDto> OrgUnitOptions { get; set; } = new();
}

public class WorkflowKpisDto
{
    /// <summary>Requests raised in the period; drafts left out.</summary>
    public int Raised { get; set; }

    public int Open { get; set; }

    public int Approved { get; set; }

    public int Rejected { get; set; }

    /// <summary>Withdrawn by the employee (status Closed).</summary>
    public int Withdrawn { get; set; }

    public int Transferred { get; set; }

    /// <summary>Approved ÷ (approved + rejected) × 100; empty when nothing was decided.</summary>
    public double? ApprovalRatePercent { get; set; }

    public int Overdue { get; set; }

    /// <summary>Average days from raising a request to its final approval or rejection.</summary>
    public double? AverageDaysToDecide { get; set; }

    /// <summary>Days the longest-waiting open request has waited since its last action.</summary>
    public double? LongestWaitDays { get; set; }
}

public class WorkflowRequestTypeOptionDto
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}

public class WorkflowStatusCountDto
{
    public RequestStatus Status { get; set; }

    public int Count { get; set; }
}

public class WorkflowTrendPointDto
{
    public DateTime MonthStart { get; set; }

    public List<WorkflowStatusCountDto> Counts { get; set; } = new();
}

public class WorkflowTypeBucketDto
{
    public string RequestType { get; set; }

    public int Raised { get; set; }

    public int Open { get; set; }

    public int Approved { get; set; }

    public int Rejected { get; set; }

    public int Overdue { get; set; }

    public double? AverageDaysToDecide { get; set; }
}

public class WorkflowOrgUnitBucketDto
{
    public long? OrganizationUnitId { get; set; }

    public string Name { get; set; }

    public bool IsDirectStaff { get; set; }

    public bool HasChildren { get; set; }

    public int Raised { get; set; }

    public int Open { get; set; }

    public int Overdue { get; set; }

    public double? AverageDaysToDecide { get; set; }
}

public class WorkflowApproverBucketDto
{
    public int ApproverId { get; set; }

    public string ApproverCode { get; set; }

    public string ApproverNameEn { get; set; }

    public string ApproverNameAr { get; set; }

    public int Waiting { get; set; }

    public int Overdue { get; set; }

    public double OldestDays { get; set; }
}

public class WorkflowWaitingBandDto
{
    public int Index { get; set; }

    /// <summary>Lower bound in days, included.</summary>
    public int FromDays { get; set; }

    /// <summary>Upper bound in days, included; empty for the top band.</summary>
    public int? ToDays { get; set; }

    public int Requests { get; set; }
}

public class WorkflowRequestRowDto
{
    public int Id { get; set; }

    public string RequestNumber { get; set; }

    public string RequestType { get; set; }

    public int? EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public string OrganizationUnitName { get; set; }

    public DateTime CreationTime { get; set; }

    public RequestStatus Status { get; set; }

    public int WorkflowStep { get; set; }

    /// <summary>Who it is waiting with, for an open request.</summary>
    public string WaitingWithEn { get; set; }

    public string WaitingWithAr { get; set; }

    /// <summary>Open: days since the last action. Decided: days it took.</summary>
    public double? Days { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsOverdue { get; set; }
}
