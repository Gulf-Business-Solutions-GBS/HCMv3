using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeSeparationDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime SubmissionDate { get; set; }
    public DateTime LastDayInService { get; set; }
    public int NoticePeriodDays { get; set; }
    public DateTime PlannedLastDayInService { get; set; }
    public SeparationType SeparationType { get; set; }
    public int SeparationReasonId { get; set; }
    public string SeparationReasonName { get; set; }
    public DateTime? RevokeDate { get; set; }
}

public class CreateOrEditEmployeeSeparationDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime SubmissionDate { get; set; }

    [Required]
    public DateTime LastDayInService { get; set; }

    public int NoticePeriodDays { get; set; }

    public DateTime PlannedLastDayInService { get; set; }

    [Required]
    public SeparationType SeparationType { get; set; }

    [Required]
    public int SeparationReasonId { get; set; }

    public DateTime? RevokeDate { get; set; }
}

public class GetEmployeeSeparationForEditOutput
{
    public CreateOrEditEmployeeSeparationDto EmployeeSeparation { get; set; }
}

public class GetAllEmployeeSeparationsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "SubmissionDate DESC";
        }
    }
}
