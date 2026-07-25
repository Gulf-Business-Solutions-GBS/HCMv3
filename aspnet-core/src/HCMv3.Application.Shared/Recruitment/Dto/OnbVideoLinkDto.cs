using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="OnbVideoLink"/>.
/// </summary>
public class OnbVideoLinkDto : EntityDto<int>
{
    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string Link { get; set; }

    public bool IsActive { get; set; }
}
