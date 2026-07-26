using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.Workflow.Dto;

public class EmployeeWorkflowRoleDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int WorkflowRoleId { get; set; }
    public string WorkflowRoleNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
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

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
