using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class NotificationSettingsListDto : EntityDto
{
    public bool DisableNotificationWindow { get; set; }

    public AdvancePaymentReminderDateType AdvancePaymentReminderDateType { get; set; }
}

public class NotificationSettingsEditDto : EntityDto
{
    public bool DisableNotificationWindow { get; set; }

    public AdvancePaymentReminderDateType AdvancePaymentReminderDateType { get; set; }
}
