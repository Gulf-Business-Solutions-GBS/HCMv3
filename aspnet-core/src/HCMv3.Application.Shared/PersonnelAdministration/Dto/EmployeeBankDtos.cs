using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeBankDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int? BankId { get; set; }
    public string BankNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? BankCountryId { get; set; }
    public string BankCountryNameEn { get; set; }
    public PaymentType PaymentType { get; set; }
    public string AccountNum { get; set; }
    public int? PaymentCurrencyId { get; set; }
    public string PaymentCurrencyNameEn { get; set; }
    public string IBAN { get; set; }
    public string BankBranch { get; set; }
    public int? PayerBankId { get; set; }
    public string PayerBankNameEn { get; set; }
}

public class CreateOrEditEmployeeBankDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int? BankId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? BankCountryId { get; set; }

    public PaymentType PaymentType { get; set; }

    [StringLength(256)]
    public string AccountNum { get; set; }

    public int? PaymentCurrencyId { get; set; }

    [StringLength(256)]
    public string IBAN { get; set; }

    [StringLength(256)]
    public string BankBranch { get; set; }

    public int? PayerBankId { get; set; }
}

public class GetEmployeeBankForEditOutput
{
    public CreateOrEditEmployeeBankDto EmployeeBank { get; set; }
}

public class GetAllEmployeeBanksInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
