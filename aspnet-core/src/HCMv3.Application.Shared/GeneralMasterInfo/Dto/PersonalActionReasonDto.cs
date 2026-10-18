using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="PersonalActionReason"/>.
/// </summary>
public class PersonalActionReasonDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int? PersonalActionId { get; set; }

    public string PersonalActionName { get; set; }

    public int? NotificationConfigId { get; set; }

    public string NotificationConfigName { get; set; }
}
