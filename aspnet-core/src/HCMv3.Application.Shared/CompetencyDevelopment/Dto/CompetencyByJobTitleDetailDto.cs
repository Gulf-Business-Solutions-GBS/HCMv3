using Abp.Application.Services.Dto;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Read/list DTO for <see cref="CompetencyByJobTitleDetail"/>.</summary>
public class CompetencyByJobTitleDetailDto : EntityDto<int>
{
    public int CompetencyByJobTitleId { get; set; }

    public int CompetencyId { get; set; }

    public string CompetencyName { get; set; }

    public double Weight { get; set; }

    public string Target { get; set; }
}
