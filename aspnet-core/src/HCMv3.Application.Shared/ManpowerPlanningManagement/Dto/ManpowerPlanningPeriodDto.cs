using System;
using Abp.Application.Services.Dto;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="ManpowerPlanningPeriod"/>.
/// </summary>
public class ManpowerPlanningPeriodDto : EntityDto<int>
{
    public string Name { get; set; }

    public bool Active { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
