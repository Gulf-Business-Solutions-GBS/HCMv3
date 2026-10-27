using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CVSkill"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditCVSkillDto
{
    public int? Id { get; set; }

    [Required]
    public int ApplicantCVId { get; set; }

    [StringLength(256)]
    public string Skill { get; set; }

    public ProficiencyType Proficiency { get; set; }
}
