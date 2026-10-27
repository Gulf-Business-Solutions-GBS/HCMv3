using Abp.Application.Services.Dto;

namespace HCMv3.Recruitment.Dto;

/// <summary>
/// Read/list DTO for <see cref="OnbGroupDetail"/>.
/// </summary>
public class OnbGroupDetailDto : EntityDto<int>
{
    public int OnbGroupId { get; set; }

    public int? OnbTaskId { get; set; }

    public string OnbTaskNameEn { get; set; }

    public int? JobTitleId { get; set; }

    public string JobTitleNameEn { get; set; }
}
