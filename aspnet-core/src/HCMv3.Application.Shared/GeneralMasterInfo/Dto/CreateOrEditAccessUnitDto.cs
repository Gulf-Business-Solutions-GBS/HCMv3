using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AccessUnit"/>.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditAccessUnitDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code (legacy <c>GetMaxCode</c> parity).
    /// </summary>
    [StringLength(AccessUnitConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(AccessUnitConsts.MaxNameEnLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(AccessUnitConsts.MaxNameArLength)]
    public string NameAr { get; set; }

    [StringLength(AccessUnitConsts.MaxIPAddressLength)]
    public string IPAddress { get; set; }

    public int Locked { get; set; }
}
