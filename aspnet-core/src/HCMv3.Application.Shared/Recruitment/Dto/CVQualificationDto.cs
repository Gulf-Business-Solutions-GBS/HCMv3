using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="CVQualification"/>.
/// </summary>
public class CVQualificationDto : EntityDto<int>
{
    public int ApplicantCVId { get; set; }

    public int? QualificationId { get; set; }

    public string QualificationNameEn { get; set; }

    public string University { get; set; }

    public double Percentage { get; set; }

    public int PassedYear { get; set; }
}
