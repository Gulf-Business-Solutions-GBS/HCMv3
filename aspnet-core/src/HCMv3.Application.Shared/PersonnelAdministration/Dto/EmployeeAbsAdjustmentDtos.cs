using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeAbsAdjustmentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int AbsenceTypeId { get; set; }
    public string AbsenceTypeName { get; set; }
    public AdjustmentType AdjustmentType { get; set; }
    public DateTime EffectiveDate { get; set; }
    public double QuotaAdjusted { get; set; }
    public string Comments { get; set; }
    public AdjustmentPaymentType AdjustmentPaymentType { get; set; }
}

public class CreateOrEditEmployeeAbsAdjustmentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int AbsenceTypeId { get; set; }

    public AdjustmentType AdjustmentType { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    public double QuotaAdjusted { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public AdjustmentPaymentType AdjustmentPaymentType { get; set; }
}

public class GetEmployeeAbsAdjustmentForEditOutput
{
    public CreateOrEditEmployeeAbsAdjustmentDto EmployeeAbsAdjustment { get; set; }
}

public class GetAllEmployeeAbsAdjustmentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
