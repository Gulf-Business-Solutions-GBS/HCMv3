using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeePaymentDto : EntityDto<int>
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
    public string Comments { get; set; }
    public UnitType UnitType { get; set; }
    public int? CostCenterId { get; set; }
    public string CostCenterNameEn { get; set; }
}

public class CreateOrEditEmployeePaymentDto
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

    [StringLength(500)]
    public string Comments { get; set; }

    public UnitType UnitType { get; set; }

    public int? CostCenterId { get; set; }
}

public class GetEmployeePaymentForEditOutput
{
    public CreateOrEditEmployeePaymentDto EmployeePayment { get; set; }
}

public class GetAllEmployeePaymentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
