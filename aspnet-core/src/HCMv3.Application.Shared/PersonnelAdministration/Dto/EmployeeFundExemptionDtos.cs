using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeFundExemptionDto : EntityDto<int>
{
    public int EmployeeFundId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeFundExemptionDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeFundId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeFundExemptionForEditOutput
{
    public CreateOrEditEmployeeFundExemptionDto EmployeeFundExemption { get; set; }
}

public class GetAllEmployeeFundExemptionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeFundId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
