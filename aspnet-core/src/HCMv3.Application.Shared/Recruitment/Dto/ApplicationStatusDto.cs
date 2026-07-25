using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="ApplicationStatus"/>.
/// </summary>
public class ApplicationStatusDto : EntityDto<int>
{
    public int? VacancyId { get; set; }

    public string VacancyTitle { get; set; }

    public int? ApplicantCVId { get; set; }

    public string ApplicantFullNameEn { get; set; }

    public AppStatus AppStatus { get; set; }

    public string CoverLetter { get; set; }
}
