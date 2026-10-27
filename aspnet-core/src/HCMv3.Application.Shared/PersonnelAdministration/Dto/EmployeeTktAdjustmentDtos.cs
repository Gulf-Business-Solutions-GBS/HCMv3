using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeTktAdjustmentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int TicketTypeId { get; set; }
    public string TicketTypeName { get; set; }
    public AdjustmentType AdjustmentType { get; set; }
    public DateTime EffectiveDate { get; set; }
    public double QuotaAccrued { get; set; }
    public double QuotaAdjusted { get; set; }
    public TicketPaymentType TicketPaymentType { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeTktAdjustmentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int TicketTypeId { get; set; }

    public AdjustmentType AdjustmentType { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    public double QuotaAccrued { get; set; }

    public double QuotaAdjusted { get; set; }

    public TicketPaymentType TicketPaymentType { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeTktAdjustmentForEditOutput
{
    public CreateOrEditEmployeeTktAdjustmentDto EmployeeTktAdjustment { get; set; }
}

public class GetAllEmployeeTktAdjustmentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "EffectiveDate DESC";
        }
    }
}
