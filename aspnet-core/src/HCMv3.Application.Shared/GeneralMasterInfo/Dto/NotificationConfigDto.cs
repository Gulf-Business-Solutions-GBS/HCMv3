using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="NotificationConfig"/>.
/// </summary>
public class NotificationConfigDto : EntityDto<int>
{
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
