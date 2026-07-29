using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OvertimeType"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditOvertimeTypeDto
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

    public double OvertimeRate { get; set; }

    /// <summary>
    /// Legacy <c>OvertimeType.WageType</c> FK (restored wave P8). Optional -- legacy's own queries all
    /// guard with <c>Where(r =&gt; r.WageType != null)</c>.
    /// </summary>
    public int? WageTypeId { get; set; }
}
