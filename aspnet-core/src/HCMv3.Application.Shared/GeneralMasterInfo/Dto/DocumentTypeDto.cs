using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="DocumentType"/>.
/// </summary>
public class DocumentTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int NotificationPeriod { get; set; }

    public int? NotificationConfigId { get; set; }

    public string NotificationConfigName { get; set; }

    public NotificationFrequency NotificationFrequency { get; set; }

    public int NotificationCountLimit { get; set; }

    public DocumentBelongsTo DocumentBelongsTo { get; set; }
}
