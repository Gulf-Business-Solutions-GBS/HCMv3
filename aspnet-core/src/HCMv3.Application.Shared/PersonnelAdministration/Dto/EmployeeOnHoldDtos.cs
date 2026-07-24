using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeOnHoldDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool OnHold { get; set; }
    public bool OnHoldBasicPay { get; set; }
    public bool OnHoldLeavePay { get; set; }
    public bool OnHoldIndemnityPay { get; set; }
    public bool OnHoldAirTicketPay { get; set; }
    public bool OnHoldLoanDeduction { get; set; }
    public bool OnHoldAllEarnings { get; set; }
    public bool OnHoldAllDeductions { get; set; }
    public OnHoldStatus BankTransfer { get; set; }
    public string Comments { get; set; }
    public int? EmployeeActionId { get; set; }
}

public class CreateOrEditEmployeeOnHoldDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool OnHold { get; set; }

    public bool OnHoldBasicPay { get; set; }

    public bool OnHoldLeavePay { get; set; }

    public bool OnHoldIndemnityPay { get; set; }

    public bool OnHoldAirTicketPay { get; set; }

    public bool OnHoldLoanDeduction { get; set; }

    public bool OnHoldAllEarnings { get; set; }

    public bool OnHoldAllDeductions { get; set; }

    public OnHoldStatus BankTransfer { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public int? EmployeeActionId { get; set; }
}

public class GetEmployeeOnHoldForEditOutput
{
    public CreateOrEditEmployeeOnHoldDto EmployeeOnHold { get; set; }
}

public class GetAllEmployeeOnHoldsInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
