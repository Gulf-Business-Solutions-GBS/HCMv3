using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="NotificationConfig"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditNotificationConfigDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    public bool SendNotification { get; set; }

    public bool SendEmail { get; set; }

    public bool ToEmployee { get; set; }

    public bool ToRequestCreator { get; set; }

    public bool ToManager { get; set; }

    public bool SendToAll { get; set; }

    public bool SendToAllApproved { get; set; }

    public bool SendToApprover { get; set; }

    /// <summary>
    /// The template this configuration sends. Optional -- without one it notifies in the app only.
    /// </summary>
    public int? EmailTemplateId { get; set; }
}
