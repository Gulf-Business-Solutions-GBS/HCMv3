using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ClearanceRequestField"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditClearanceRequestFieldDto
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

    public int? ClearanceRequestTypeId { get; set; }

    public bool IsActive { get; set; }
}
