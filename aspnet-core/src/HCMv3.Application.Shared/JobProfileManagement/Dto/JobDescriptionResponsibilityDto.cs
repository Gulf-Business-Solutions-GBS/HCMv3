using Abp.Application.Services.Dto;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="JobDescriptionResponsibility"/>.
/// </summary>
public class JobDescriptionResponsibilityDto : EntityDto<int>
{
    public int JobDescriptionId { get; set; }

    public int JobResponsibilityId { get; set; }

    public string JobResponsibilityNameEn { get; set; }
}
