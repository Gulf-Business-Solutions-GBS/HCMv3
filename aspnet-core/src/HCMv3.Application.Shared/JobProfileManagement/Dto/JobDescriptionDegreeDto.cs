using Abp.Application.Services.Dto;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="JobDescriptionDegree"/>.
/// </summary>
public class JobDescriptionDegreeDto : EntityDto<int>
{
    public int JobDescriptionId { get; set; }

    public int EducationDegreeId { get; set; }

    public string EducationDegreeNameEn { get; set; }
}
