using Abp.Notifications;
using System;

namespace HCMv3.Notifications.Dto;

public class NotificationDetailDto
{
    public string Title { get; set; }
    public string Message { get; set; }
    public NotificationSeverity Severity { get; set; }
    public DateTime CreationTime { get; set; }
}



