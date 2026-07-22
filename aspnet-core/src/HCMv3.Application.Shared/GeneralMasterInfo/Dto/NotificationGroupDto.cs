using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="NotificationGroup"/>.
/// </summary>
public class NotificationGroupDto : EntityDto<int>
{
    public int? NotificationConfigId { get; set; }

    public string NotificationConfigName { get; set; }
}
