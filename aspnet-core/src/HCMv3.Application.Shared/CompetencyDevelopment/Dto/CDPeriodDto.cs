using Abp.Application.Services.Dto;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Read/list DTO for <see cref="CDPeriod"/>.</summary>
public class CDPeriodDto : EntityDto<int>
{
    public string Name { get; set; }

    public bool Closed { get; set; }
}
