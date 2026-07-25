using System;
using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="FirstAidBox"/>.
/// </summary>
public class FirstAidBoxDto : EntityDto<int>
{
    public string Code { get; set; }

    public int LocationId { get; set; }

    public string LocationName { get; set; }

    public DateTime AllocationDate { get; set; }

    public int ResponsiblePersonId { get; set; }

    public string ResponsiblePersonCode { get; set; }

    public string Comments { get; set; }
}
