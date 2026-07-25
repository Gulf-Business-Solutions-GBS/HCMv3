using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="CVLanguage"/>.
/// </summary>
public class CVLanguageDto : EntityDto<int>
{
    public int ApplicantCVId { get; set; }

    public int? LanguageInfoId { get; set; }

    public string LanguageInfoNameEn { get; set; }

    public ProficiencyType Proficiency { get; set; }
}
