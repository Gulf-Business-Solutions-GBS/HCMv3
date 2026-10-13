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

    /// <summary>The report a letter of this type prints as, when one was chosen. See <see cref="DocumentType.PrintReportId"/>.</summary>
    public int? PrintReportId { get; set; }

    public string PrintReportNameEn { get; set; }

    public string PrintReportNameAr { get; set; }

    /// <summary>Employees may ask to take a document of this type out of custody. See <see cref="DocumentType.IsWithdrawable"/>.</summary>
    public bool IsWithdrawable { get; set; }

    /// <summary>Offered to employees raising a request. See <see cref="DocumentType.IncludeInWorkflow"/>.</summary>
    public bool IncludeInWorkflow { get; set; }

    /// <summary>Kept out of the employee's own view. See <see cref="DocumentType.IsHiddenFromEmployee"/>.</summary>
    public bool IsHiddenFromEmployee { get; set; }

    /// <summary>A document of this type must carry an expiry date. See <see cref="DocumentType.IsExpiryDateRequired"/>.</summary>
    public bool IsExpiryDateRequired { get; set; }

    /// <summary>An employee may hold two documents of this type whose dates overlap. See <see cref="DocumentType.AllowDateOverlap"/>.</summary>
    public bool AllowDateOverlap { get; set; }
}
