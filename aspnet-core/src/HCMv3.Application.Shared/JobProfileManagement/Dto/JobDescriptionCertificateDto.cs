using Abp.Application.Services.Dto;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="JobDescriptionCertificate"/>.
/// </summary>
public class JobDescriptionCertificateDto : EntityDto<int>
{
    public int JobDescriptionId { get; set; }

    public int EducationCertificateId { get; set; }

    public string EducationCertificateNameEn { get; set; }
}
