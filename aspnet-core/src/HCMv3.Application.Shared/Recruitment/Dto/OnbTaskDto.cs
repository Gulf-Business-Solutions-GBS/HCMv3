using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="OnbTask"/>.
/// </summary>
public class OnbTaskDto : EntityDto<int>
{
    public string TaskNameEn { get; set; }

    public string TaskNameAr { get; set; }

    public int? OnbTemplateId { get; set; }

    public string OnbTemplateTitle { get; set; }

    public string Guideline { get; set; }

    public int DueDays { get; set; }
}
