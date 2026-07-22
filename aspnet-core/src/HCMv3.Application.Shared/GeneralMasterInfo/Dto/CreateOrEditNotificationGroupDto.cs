namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="NotificationGroup"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditNotificationGroupDto
{
    public int? Id { get; set; }

    public int? NotificationConfigId { get; set; }
}
