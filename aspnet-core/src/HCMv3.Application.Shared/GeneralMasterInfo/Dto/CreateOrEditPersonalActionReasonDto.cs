using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="PersonalActionReason"/>.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditPersonalActionReasonDto
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

    public int? PersonalActionId { get; set; }

    /// <summary>Legacy <c>NotificationConfig</c>: sent when an action with this reason is approved. Optional.</summary>
    public int? NotificationConfigId { get; set; }
}
