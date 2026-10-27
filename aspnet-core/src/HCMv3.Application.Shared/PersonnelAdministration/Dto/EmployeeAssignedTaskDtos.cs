using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// A row of the assigned-task grid (legacy <c>TaskRequest</c>). Nothing to do with
/// <c>EmployeeTaskDto</c>, which is the HR reminder record -- see <c>EmployeeAssignedTask</c>.
/// </summary>
public class EmployeeAssignedTaskDto : EntityDto<int>
{
    /// <summary>The ASSIGNEE. Who raised it is <see cref="CreatorUserId"/>.</summary>
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public string Subject { get; set; }

    public string Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public AssignedTaskStatus TaskStatus { get; set; }

    public string Comments { get; set; }

    /// <summary>
    /// Who assigned the task. Exposed so a screen can grey out the fields only the creator may change,
    /// matching the server-side rule -- the appearance is a convenience, the rule is enforced in the app
    /// service either way. Legacy had only the appearance, which stopped nothing.
    /// </summary>
    public long? CreatorUserId { get; set; }

    /// <summary>Null when the type is not routed for approval, so the screen can show the task as simply
    /// recorded rather than implying it went through a chain.</summary>
    public HCMv3.Workflow.RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }
}

public class CreateOrEditEmployeeAssignedTaskDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    /// <summary>
    /// Bounded at 200 to match the entity. Legacy left the subject unbounded, which is a defect rather than
    /// a feature -- see <c>EmployeeAssignedTask.Subject</c>.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Subject { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public AssignedTaskStatus TaskStatus { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}

public class GetEmployeeAssignedTaskForEditOutput
{
    public CreateOrEditEmployeeAssignedTaskDto EmployeeAssignedTask { get; set; }

    /// <summary>
    /// Whether the CURRENT user may still change the subject, description and dates. False for the assignee
    /// once the task is in the approval chain -- the same verdict the app service will reach on save, sent up
    /// front so the form can say so rather than failing on submit.
    /// </summary>
    public bool CanEditTaskDetails { get; set; }
}

public class GetAllEmployeeAssignedTasksInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Filters on the ASSIGNEE.</summary>
    public int? EmployeeId { get; set; }

    public AssignedTaskStatus? TaskStatus { get; set; }

    /// <summary>Free text over the subject.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
