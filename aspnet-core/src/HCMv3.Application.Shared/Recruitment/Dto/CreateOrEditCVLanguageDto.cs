using System.ComponentModel.DataAnnotations;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CVLanguage"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditCVLanguageDto
{
    public int? Id { get; set; }

    [Required]
    public int ApplicantCVId { get; set; }

    public int? LanguageInfoId { get; set; }

    public ProficiencyType Proficiency { get; set; }
}
