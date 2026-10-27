using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeDateDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int? DateTypeId { get; set; }
    public string DateTypeName { get; set; }
    public DateTime DateData { get; set; }
    public double DayData { get; set; }
}

public class CreateOrEditEmployeeDateDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int? DateTypeId { get; set; }

    [Required]
    public DateTime DateData { get; set; }

    public double DayData { get; set; }
}

public class GetEmployeeDateForEditOutput
{
    public CreateOrEditEmployeeDateDto EmployeeDate { get; set; }
}

public class GetAllEmployeeDatesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id ASC";
        }
    }
}
