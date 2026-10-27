using Abp.Application.Services.Dto;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="JobDescriptionCompetency"/>.
/// </summary>
public class JobDescriptionCompetencyDto : EntityDto<int>
{
    public int JobDescriptionId { get; set; }

    public int CompetencyId { get; set; }

    public string CompetencyNameEn { get; set; }
}
