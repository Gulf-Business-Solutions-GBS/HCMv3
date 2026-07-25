using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="OnbTemplate"/>.
/// </summary>
public class OnbTemplateDto : EntityDto<int>
{
    public long TemplateId { get; set; }

    public string TemplateTitle { get; set; }
}
