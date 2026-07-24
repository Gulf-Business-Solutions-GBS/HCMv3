using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeRemunerationDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int? WageTypeId { get; set; }
    public string WageTypeNameEn { get; set; }
    public DateTime PaymentDate { get; set; }
    public double PaymentNumber { get; set; }
    public PaymentUnit PaymentUnit { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeRemunerationDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int? WageTypeId { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    public double PaymentNumber { get; set; }

    public PaymentUnit PaymentUnit { get; set; }

    public string Comments { get; set; }
}

public class GetEmployeeRemunerationForEditOutput
{
    public CreateOrEditEmployeeRemunerationDto EmployeeRemuneration { get; set; }
}

public class GetAllEmployeeRemunerationsInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
