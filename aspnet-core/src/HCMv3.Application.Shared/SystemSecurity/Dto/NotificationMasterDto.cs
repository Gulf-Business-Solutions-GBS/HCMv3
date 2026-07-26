using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class NotificationMasterDto : EntityDto<int>
{
    public string NotificationType { get; set; }
    public int NotificationConfigId { get; set; }
    public string NotificationConfigName { get; set; }
    public string Criterion { get; set; }
    public bool IsEnabled { get; set; }
}
