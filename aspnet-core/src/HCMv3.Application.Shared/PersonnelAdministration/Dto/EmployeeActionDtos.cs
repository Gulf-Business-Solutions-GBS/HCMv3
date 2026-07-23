using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeActionDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int PersonalActionId { get; set; }
    public string PersonalActionName { get; set; }
    public int ActionReasonId { get; set; }
    public string ActionReasonName { get; set; }
    public DateTime StartDate { get; set; }
    public string RecommendedBy { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeActionDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int PersonalActionId { get; set; }

    [Required]
    public int ActionReasonId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [StringLength(256)]
    public string RecommendedBy { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}

public class GetEmployeeActionForEditOutput
{
    public CreateOrEditEmployeeActionDto EmployeeAction { get; set; }
}

public class GetAllEmployeeActionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
