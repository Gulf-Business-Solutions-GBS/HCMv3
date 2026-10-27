using Abp.Application.Services.Dto;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="JobDescriptionSkill"/>.
/// </summary>
public class JobDescriptionSkillDto : EntityDto<int>
{
    public int JobDescriptionId { get; set; }

    public int SkillId { get; set; }

    public string SkillNameEn { get; set; }
}
