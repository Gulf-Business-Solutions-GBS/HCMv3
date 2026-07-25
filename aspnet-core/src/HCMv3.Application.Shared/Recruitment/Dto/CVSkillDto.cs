using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="CVSkill"/>.
/// </summary>
public class CVSkillDto : EntityDto<int>
{
    public int ApplicantCVId { get; set; }

    public string Skill { get; set; }

    public ProficiencyType Proficiency { get; set; }
}
