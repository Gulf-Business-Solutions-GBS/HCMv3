using System;
using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="FirstAidBoxDetail"/>.
/// </summary>
public class FirstAidBoxDetailDto : EntityDto<int>
{
    public int FirstAidBoxId { get; set; }

    public int MedicineId { get; set; }

    public string MedicineNameEn { get; set; }

    public DateTime ExpiryDate { get; set; }

    public DateTime AllocationDate { get; set; }

    public int AllocatedById { get; set; }

    public string AllocatedByCode { get; set; }

    public bool IsExpired { get; set; }

    public bool IsReplaced { get; set; }

    public string Comments { get; set; }
}
