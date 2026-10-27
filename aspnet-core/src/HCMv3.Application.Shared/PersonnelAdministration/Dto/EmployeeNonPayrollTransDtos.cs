using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeNonPayrollTransDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int WageTypeId { get; set; }
    public string WageTypeNameEn { get; set; }
    public double Amount { get; set; }
    public int? CurrencyId { get; set; }
    public string CurrencyNameEn { get; set; }
    public DateTime PaymentDate { get; set; }
    public bool IsLocked { get; set; }
    public string Comments { get; set; }
    public bool NotificationSent { get; set; }
    public string PayableBy { get; set; }
    public string PaymentMethod { get; set; }
}

public class CreateOrEditEmployeeNonPayrollTransDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    public double Amount { get; set; }

    public int? CurrencyId { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    public bool IsLocked { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public bool NotificationSent { get; set; }

    [StringLength(256)]
    public string PayableBy { get; set; }

    [StringLength(256)]
    public string PaymentMethod { get; set; }
}

public class GetEmployeeNonPayrollTransForEditOutput
{
    public CreateOrEditEmployeeNonPayrollTransDto EmployeeNonPayrollTrans { get; set; }
}

public class GetAllEmployeeNonPayrollTransInput : PagedAndSortedResultRequestDto, IShouldNormalize
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

/// <summary>A wage type a non-payroll transaction may use (legacy <c>NonPayrollWageType</c>).</summary>
public class NonPayrollWageTypeDto : EntityDto
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
