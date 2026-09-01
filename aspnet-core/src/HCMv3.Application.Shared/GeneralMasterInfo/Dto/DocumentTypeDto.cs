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

    /// <summary>
    /// Whether this type is one the company issues as a LETTER (salary certificate, experience certificate)
    /// rather than a document it merely records (passport, civil ID). Drives the letter-request dropdown.
    /// See <see cref="DocumentType.IsLetterType"/> for why this is the inverse of legacy's AllowUpdate.
    /// </summary>
    public bool IsLetterType { get; set; }
}
