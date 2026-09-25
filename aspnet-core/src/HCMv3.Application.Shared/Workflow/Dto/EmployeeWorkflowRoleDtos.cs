using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Workflow.Dto;

public class EmployeeWorkflowRoleDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    /// <summary>
    /// The employee's staff number. Carried alongside the name because an administrator setting up approvals
    /// is working from a number -- two people share a name far more often than they share a code.
    /// </summary>
    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }
    public int WorkflowRoleId { get; set; }
    public string WorkflowRoleNameEn { get; set; }
    public string WorkflowRoleNameAr { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Whether this assignment is in force today.
    ///
    /// <para>
    /// The one thing worth showing in a list of these. An assignment outside its date range is invisible to
    /// approver resolution, so a chain pointing at the role finds nobody -- and the row still sits in the
    /// grid looking correct. Saying so per row is the difference between "I assigned it" and "it is live".
    /// </para>
    /// </summary>
    public bool IsActiveToday { get; set; }
}

public class CreateOrEditEmployeeWorkflowRoleDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int WorkflowRoleId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class GetEmployeeWorkflowRoleForEditOutput
{
    public CreateOrEditEmployeeWorkflowRoleDto EmployeeWorkflowRole { get; set; }
}

public class GetAllEmployeeWorkflowRolesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Narrows to the holders of one role -- "who is the HR approver?", which is the question this screen
    /// mostly gets opened to answer.
    /// </summary>
    public int? WorkflowRoleId { get; set; }

    /// <summary>
    /// Free text over the employee's code and English name, and the role's English name.
    /// </summary>
    public string Filter { get; set; }

    /// <summary>
    /// When true, keeps only the assignments in force today -- the ones approver resolution can actually see.
    /// </summary>
    public bool? OnlyActiveToday { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
