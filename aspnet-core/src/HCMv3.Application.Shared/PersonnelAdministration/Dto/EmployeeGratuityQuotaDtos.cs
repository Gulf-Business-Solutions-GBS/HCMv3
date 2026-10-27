using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeGratuityQuotaDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime EntitlementFrom { get; set; }
    public DateTime EntitlementTo { get; set; }
    public DateTime DeductionFrom { get; set; }
    public DateTime DeductionTo { get; set; }
    public double QuotaAccrued { get; set; }
    public double QuotaUsed { get; set; }
    public double QuotaAdjusted { get; set; }

    /// <summary>Computed: Math.Round(QuotaAccrued - (QuotaUsed + QuotaAdjusted), 3).</summary>
    public double QuotaBalance { get; set; }
}

public class CreateOrEditEmployeeGratuityQuotaDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime EntitlementFrom { get; set; }

    [Required]
    public DateTime EntitlementTo { get; set; }

    [Required]
    public DateTime DeductionFrom { get; set; }

    [Required]
    public DateTime DeductionTo { get; set; }

    public double QuotaAccrued { get; set; }

    public double QuotaUsed { get; set; }

    public double QuotaAdjusted { get; set; }
}

public class GetEmployeeGratuityQuotaForEditOutput
{
    public CreateOrEditEmployeeGratuityQuotaDto EmployeeGratuityQuota { get; set; }
}

public class GetAllEmployeeGratuityQuotasInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "EntitlementFrom DESC";
        }
    }
}
