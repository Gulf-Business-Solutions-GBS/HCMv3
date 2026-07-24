using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeAttendanceQuotaDto : EntityDto<int>
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

    /// <summary>Computed: QuotaAccrued - QuotaUsed.</summary>
    public double QuotaBalance { get; set; }
}

public class CreateOrEditEmployeeAttendanceQuotaDto
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
}

public class GetEmployeeAttendanceQuotaForEditOutput
{
    public CreateOrEditEmployeeAttendanceQuotaDto EmployeeAttendanceQuota { get; set; }
}

public class GetAllEmployeeAttendanceQuotasInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
