using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeFundDetailDto : EntityDto<int>
{
    public int EmployeeFundId { get; set; }
    public DateTime TransactionDate { get; set; }
    public double Amount { get; set; }
    public FundPayType FundPayType { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeFundDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeFundId { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    public double Amount { get; set; }

    public FundPayType FundPayType { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeFundDetailForEditOutput
{
    public CreateOrEditEmployeeFundDetailDto EmployeeFundDetail { get; set; }
}

public class GetAllEmployeeFundDetailsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeFundId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "TransactionDate DESC";
        }
    }
}
