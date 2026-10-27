using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeTicketQuotaDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int TicketTypeId { get; set; }
    public string TicketTypeName { get; set; }
    public DateTime EntitlementFrom { get; set; }
    public DateTime EntitlementTo { get; set; }
    public DateTime DeductionFrom { get; set; }
    public DateTime DeductionTo { get; set; }
    public double QuotaAccrued { get; set; }
    public double QuotaUsed { get; set; }
    public double QuotaAdjusted { get; set; }

    /// <summary>Computed: QuotaAccrued - (QuotaUsed + QuotaAdjusted).</summary>
    public double QuotaBalance { get; set; }
}

public class CreateOrEditEmployeeTicketQuotaDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int TicketTypeId { get; set; }

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

public class GetEmployeeTicketQuotaForEditOutput
{
    public CreateOrEditEmployeeTicketQuotaDto EmployeeTicketQuota { get; set; }
}

public class GetAllEmployeeTicketQuotasInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
