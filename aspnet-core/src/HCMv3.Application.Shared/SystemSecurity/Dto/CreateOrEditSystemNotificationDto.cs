using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditSystemNotificationDto
{
    public int? Id { get; set; }

    [StringLength(256)]
    public string NotificationName { get; set; }

    [Required]
    public int NotificationConfigId { get; set; }

    public int ReminderDays { get; set; }

    public bool IsActive { get; set; }
}
