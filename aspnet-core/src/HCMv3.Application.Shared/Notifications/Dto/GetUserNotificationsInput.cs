using System;
using Abp.Notifications;
using HCMv3.Dto;

namespace HCMv3.Notifications.Dto;

public class GetUserNotificationsInput : PagedInputDto
{
    public UserNotificationState? State { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

