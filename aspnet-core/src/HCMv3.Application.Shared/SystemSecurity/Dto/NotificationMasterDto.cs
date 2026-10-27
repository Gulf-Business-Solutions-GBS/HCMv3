using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class NotificationMasterDto : EntityDto<int>
{
    public string NotificationType { get; set; }

    /// <summary>The type's display name, or the stored text itself when it is not a supported type.</summary>
    public string NotificationTypeName { get; set; }

    /// <summary>False for a rule whose type is not a supported source -- such a rule is skipped until re-entered.</summary>
    public bool IsSupportedType { get; set; }

    public int? NotificationSubTypeId { get; set; }

    public string NotificationSubTypeName { get; set; }

    public int NotificationConfigId { get; set; }
    public string NotificationConfigName { get; set; }
    public string Criterion { get; set; }
    public bool IsEnabled { get; set; }
}
