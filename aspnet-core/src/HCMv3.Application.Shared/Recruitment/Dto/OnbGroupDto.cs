using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="OnbGroup"/>.
/// </summary>
public class OnbGroupDto : EntityDto<int>
{
    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
