using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>One row of the overtime batch list.</summary>
public class OvertimeBatchListDto : EntityDto<int>
{
    public long OrganizationUnitId { get; set; }

    public string OrganizationUnitName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }

    public int LineCount { get; set; }

    public double TotalApprovedHours { get; set; }

    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }

    public string RequestNumber { get; set; }

    /// <summary>True once the overtime rows have been written (approval, or a save with workflow off).</summary>
    public bool IsPosted { get; set; }
}

/// <summary>
/// One employee-day: either a candidate loaded from the time register, or a saved batch line. Same shape both
/// ways so the screen renders one grid.
/// </summary>
public class OvertimeBatchLineDto
{
    /// <summary>The saved line's id; null for a candidate that has not been saved.</summary>
    public int? Id { get; set; }

    public int? TimeRegisterId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeNameEn { get; set; }

    public string EmployeeNameAr { get; set; }

    public DateTime Date { get; set; }

    public DateTime? ShiftStart { get; set; }

    public DateTime? ShiftEnd { get; set; }

    public DateTime? SignIn { get; set; }

    public DateTime? SignOut { get; set; }

    public double PlannedHours { get; set; }

    /// <summary>Overtime hours the day produced, from the time register.</summary>
    public double TotalHours { get; set; }

    public double LateIn { get; set; }

    public double EarlyOut { get; set; }

    public double ApprovedHours { get; set; }

    public int? OvertimeTypeId { get; set; }

    public string OvertimeTypeNameEn { get; set; }

    public string OvertimeTypeNameAr { get; set; }

    public string Comments { get; set; }

    /// <summary>The employee already has an overtime row for this day; the line cannot be put forward.</summary>
    public bool HasExistingOvertime { get; set; }

    /// <summary>The overtime row approval created from this line, once it has.</summary>
    public int? EmployeeOvertimeId { get; set; }
}

/// <summary>
/// What the screen sends per line. Only the decision travels: the time figures are re-read from the time
/// register on the server, because the overtime hours become the overtime row's actual hours on approval.
/// </summary>
public class CreateOrEditOvertimeBatchLineDto
{
    [Required]
    public int TimeRegisterId { get; set; }

    [Range(0, 24)]
    public double ApprovedHours { get; set; }

    public int? OvertimeTypeId { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}

public class CreateOrEditOvertimeBatchDto
{
    public int? Id { get; set; }

    [Required]
    public long OrganizationUnitId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>Legacy <c>RequestComments</c>.</summary>
    [StringLength(4000)]
    public string Comments { get; set; }

    /// <summary>
    /// Every grid row may be sent; only rows with approved hours above zero or a comment are kept, exactly as
    /// legacy's submit kept them.
    /// </summary>
    public List<CreateOrEditOvertimeBatchLineDto> Details { get; set; }
}

public class GetOvertimeBatchForEditOutput
{
    public CreateOrEditOvertimeBatchDto OvertimeBatch { get; set; }

    public List<OvertimeBatchLineDto> Details { get; set; }

    public string OrganizationUnitName { get; set; }

    public RequestStatus? WorkflowStatus { get; set; }

    public string RequestNumber { get; set; }

    /// <summary>Not routed yet, or sent back: nobody is being asked to agree to it right now.</summary>
    public bool IsEditable { get; set; }

    /// <summary>Sent back to the requester, so it can be submitted again.</summary>
    public bool CanResubmit { get; set; }
}

public class GetOvertimeBatchCandidatesInput
{
    [Required]
    public long OrganizationUnitId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Legacy setting <c>AllowOvertimeHoursOnly</c>: list only days on which the time register calculated
    /// overtime. Off lists every day of every employee in the unit.
    /// </summary>
    public bool OnlyOvertimeDays { get; set; } = true;

    /// <summary>When editing a batch, its own lines must not count as "already in another batch".</summary>
    public int? ExcludeOvertimeBatchId { get; set; }
}

public class GetAllOvertimeBatchesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public long? OrganizationUnitId { get; set; }

    public RequestStatus? WorkflowStatus { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}

public class OvertimeBatchOrganizationUnitDto : EntityDto<long>
{
    public string Code { get; set; }

    public string DisplayName { get; set; }
}

public class OvertimeBatchOvertimeTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
