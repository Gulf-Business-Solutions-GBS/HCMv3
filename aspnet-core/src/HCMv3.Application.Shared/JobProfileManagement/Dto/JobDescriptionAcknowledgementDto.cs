using Abp.Application.Services.Dto;

namespace HCMv3.JobProfileManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="JobDescriptionAcknowledgement"/>.
/// </summary>
public class JobDescriptionAcknowledgementDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeNameEn { get; set; }

    public int JobTitleId { get; set; }

    public string JobTitleNameEn { get; set; }

    public int JobDescriptionId { get; set; }

    public string JobDescriptionShortDescriptionEn { get; set; }

    public string Version { get; set; }

    public bool Acknowledged { get; set; }
}
