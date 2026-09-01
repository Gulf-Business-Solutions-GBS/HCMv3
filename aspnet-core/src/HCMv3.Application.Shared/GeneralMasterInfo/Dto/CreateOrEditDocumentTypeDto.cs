using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="DocumentType"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditDocumentTypeDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code (legacy <c>GetMaxCode</c> parity).
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public int NotificationPeriod { get; set; }

    public int? NotificationConfigId { get; set; }

    public NotificationFrequency NotificationFrequency { get; set; }

    public int NotificationCountLimit { get; set; }

    public DocumentBelongsTo DocumentBelongsTo { get; set; }

    /// <summary>
    /// Tick to offer this type on the letter-request screen. Defaults to false, so a newly created document
    /// type is not silently requestable as a letter until somebody says it is.
    /// </summary>
    public bool IsLetterType { get; set; }
}
