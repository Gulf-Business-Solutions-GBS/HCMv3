using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="OnbTemplateDetails"/>.
/// </summary>
public class OnbTemplateDetailsDto : EntityDto<int>
{
    public int OnbTemplateId { get; set; }

    public long TemplateId { get; set; }

    public string TemplateData { get; set; }
}
