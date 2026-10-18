using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="WorkLocation"/>.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditWorkLocationDto
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

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double GeoRange { get; set; }

    [StringLength(1000)]
    public string Address { get; set; }

    [StringLength(2000)]
    public string GeoFenceCoordinates { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Legacy <c>WorkLocation.Sponsor</c>. Optional.</summary>
    public int? SponsorId { get; set; }
}
