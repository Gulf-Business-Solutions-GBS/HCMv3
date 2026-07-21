using System.Collections.Generic;

namespace HCMv3.Notifications.Dto;

public class GetNotificationSettingsOutput
{
    public bool ReceiveNotifications { get; set; }

    public List<NotificationSubscriptionWithDisplayNameDto> Notifications { get; set; }
}

