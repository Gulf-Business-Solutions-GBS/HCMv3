using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeGratuityDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime GratuityPaidDate { get; set; }
    public double GratuityPaidDays { get; set; }
    public bool ResetGratuityCalc { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeGratuityDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime GratuityPaidDate { get; set; }

    public double GratuityPaidDays { get; set; }

    public bool ResetGratuityCalc { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeGratuityForEditOutput
{
    public CreateOrEditEmployeeGratuityDto EmployeeGratuity { get; set; }
}

public class GetAllEmployeeGratuitiesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "GratuityPaidDate DESC";
        }
    }
}
