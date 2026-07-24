using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeFnFPaymentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int? EmployeeActionId { get; set; }
    public int? EmployeeBasicPayDetailId { get; set; }
    public DateTime CompensationDate { get; set; }
    public double LeaveCompensateDays { get; set; }
    public double GratuityCompensateDays { get; set; }
    public double IndemnityCompensateAmount { get; set; }
    public double IndemnityPayAdjustment { get; set; }
    public double TicketCompensateCount { get; set; }
    public double Dependant1TicketCompensateCount { get; set; }
    public double Dependant2TicketCompensateCount { get; set; }
    public double Dependant3TicketCompensateCount { get; set; }
    public double CompensateLoanAmount { get; set; }
    public double CompensateBonusPay { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeFnFPaymentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int? EmployeeActionId { get; set; }

    public int? EmployeeBasicPayDetailId { get; set; }

    [Required]
    public DateTime CompensationDate { get; set; }

    public double LeaveCompensateDays { get; set; }

    public double GratuityCompensateDays { get; set; }

    public double IndemnityCompensateAmount { get; set; }

    public double IndemnityPayAdjustment { get; set; }

    public double TicketCompensateCount { get; set; }

    public double Dependant1TicketCompensateCount { get; set; }

    public double Dependant2TicketCompensateCount { get; set; }

    public double Dependant3TicketCompensateCount { get; set; }

    public double CompensateLoanAmount { get; set; }

    public double CompensateBonusPay { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }
}

public class GetEmployeeFnFPaymentForEditOutput
{
    public CreateOrEditEmployeeFnFPaymentDto EmployeeFnFPayment { get; set; }
}

public class GetAllEmployeeFnFPaymentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "CompensationDate DESC";
        }
    }
}
