using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Dashboards.Dto;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>HR's view of a leave plan, including the workflow status so pending plans can be told apart.</summary>
public class LeavePlanDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public string EmployeeFullNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double PlannedDays { get; set; }

    public double? ProjectedLeaveBalance { get; set; }

    public string Comments { get; set; }

    /// <summary>Null when HR recorded it, or the type had workflow switched off.</summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }

    public string RequestNumber { get; set; }

    public DateTime CreationTime { get; set; }
}

public class GetAllLeavePlansInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Employee code or name contains.</summary>
    public string Filter { get; set; }

    public int? EmployeeId { get; set; }

    /// <summary>Plans that end on or after this date.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Plans that start on or before this date.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Include plans still in flight (and the employee's own drafts are never shown -- they are nobody else's
    /// business until sent). On by default for this screen: a plan is information, and a pending one is
    /// exactly what a manager wants to see coming.
    /// </summary>
    public bool IncludePending { get; set; } = true;

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }

        if (MaxResultCount <= 0)
        {
            MaxResultCount = 10;
        }
    }
}

/// <summary>
/// What a day cell of the planner holds. Flags, because one employee can have a plan and a leave on the same
/// day and the planner shows both -- that overlap is the very thing it is there to reveal.
/// </summary>
[Flags]
public enum LeavePlannerDayFlags
{
    None = 0,
    PlannedApproved = 1,
    PlannedPending = 2,
    LeaveApproved = 4,
    LeavePending = 8
}

/// <summary>Which kind of entry an item on the planner is.</summary>
public enum LeavePlannerEntryKind
{
    PlannedApproved = 0,
    PlannedPending = 1,
    LeaveApproved = 2,
    LeavePending = 3
}

/// <summary>
/// The Leave Planner (legacy <c>uscLeavePlanner</c>, Reports > Analysis > Transaction): who plans to be away
/// when, by organization unit, for one month. The four switches are legacy's four check boxes, with their
/// legacy captions.
/// </summary>
public class GetLeavePlannerInput
{
    /// <summary>Any day in the month to show; the planner always covers the whole calendar month.</summary>
    public DateTime? Month { get; set; }

    /// <summary>Only this unit and the units below it (legacy "Department").</summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>Employee code or name contains (legacy "Employee").</summary>
    public string Filter { get; set; }

    /// <summary>Legacy "Show Leave Planner Approved".</summary>
    public bool IncludePlanned { get; set; } = true;

    /// <summary>Legacy "Show Leave Planner Unapproved" -- plans still with an approver.</summary>
    public bool IncludePlannedPending { get; set; }

    /// <summary>Legacy "Show Actual Leave Approved" (on by default in legacy too).</summary>
    public bool IncludeLeave { get; set; } = true;

    /// <summary>Legacy "Show Actual Leave Unapproved" -- leave still with an approver.</summary>
    public bool IncludeLeavePending { get; set; }
}

public class LeavePlannerDto
{
    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    /// <summary>One per organization unit that has anybody away this month; employees with no unit last.</summary>
    public List<LeavePlannerGroupDto> Groups { get; set; } = new();

    /// <summary>Whose data this is. Null on the self-service team view, where it is always "me and my team".</summary>
    public DashboardScopeDto Scope { get; set; }

    /// <summary>The units the unit filter may offer -- those the in-scope employees sit in, with their parents.</summary>
    public List<DashboardOrgUnitOptionDto> OrgUnitOptions { get; set; } = new();
}

public class LeavePlannerGroupDto
{
    /// <summary>Null for employees with no current organization unit.</summary>
    public long? OrganizationUnitId { get; set; }

    public string OrganizationUnitName { get; set; }

    /// <summary>For each day of the month, how many employees in this unit have something on it.</summary>
    public List<int> AwayCountByDay { get; set; } = new();

    public List<LeavePlannerRowDto> Rows { get; set; } = new();
}

public class LeavePlannerRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public string EmployeeFullNameAr { get; set; }

    /// <summary>One per day of the month, from <see cref="LeavePlannerDto.FromDate"/>.</summary>
    public List<LeavePlannerDayFlags> Days { get; set; } = new();

    /// <summary>The plans and leave behind the cells, for the tooltip and the legend.</summary>
    public List<LeavePlannerEntryDto> Entries { get; set; } = new();
}

public class LeavePlannerEntryDto
{
    public LeavePlannerEntryKind Kind { get; set; }

    /// <summary>The <c>LeavePlan</c> or <c>EmployeeAbsence</c> id, depending on <see cref="Kind"/>.</summary>
    public int Id { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>The leave type for a leave; empty for a plan, which names no type.</summary>
    public string LeaveTypeNameEn { get; set; }

    public string LeaveTypeNameAr { get; set; }
}
