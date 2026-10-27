using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditNotificationMasterDto
{
    public int? Id { get; set; }

    /// <summary>A rule source code (the type picker), e.g. "EmployeeDocument".</summary>
    [Required]
    [StringLength(256)]
    public string NotificationType { get; set; }

    /// <summary>The sub type (document type, absence type...), or null for every sub type.</summary>
    public int? NotificationSubTypeId { get; set; }

    [Required]
    public int NotificationConfigId { get; set; }

    /// <summary>The criterion as the business rule formula editor's expression JSON. Blank matches every record.</summary>
    public string Criterion { get; set; }

    public bool IsEnabled { get; set; }
}
