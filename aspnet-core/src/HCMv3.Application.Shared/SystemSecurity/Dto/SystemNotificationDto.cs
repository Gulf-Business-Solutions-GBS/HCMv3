using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class SystemNotificationDto : EntityDto<int>
{
    public string NotificationName { get; set; }
    public int NotificationConfigId { get; set; }
    public string NotificationConfigName { get; set; }
    public int ReminderDays { get; set; }
    public bool IsActive { get; set; }
}
