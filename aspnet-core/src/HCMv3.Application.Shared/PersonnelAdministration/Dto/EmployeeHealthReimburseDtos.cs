using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeHealthReimburseDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime PaymentDate { get; set; }
    public double Amount { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeHealthReimburseDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    public double Amount { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeHealthReimburseForEditOutput
{
    public CreateOrEditEmployeeHealthReimburseDto EmployeeHealthReimburse { get; set; }
}

public class GetAllEmployeeHealthReimbursesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "PaymentDate DESC";
        }
    }
}
