using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditNotificationMasterDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string NotificationType { get; set; }

    [Required]
    public int NotificationConfigId { get; set; }

    public string Criterion { get; set; }

    public bool IsEnabled { get; set; }
}
