using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SponsorLicenseInfo"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditSponsorLicenseInfoDto
{
    public int? Id { get; set; }

    [Required]
    public int SponsorId { get; set; }

    /// <summary>"Trade License Number" in the legacy UI.</summary>
    [Required]
    [StringLength(256)]
    public string TradeLicenseNum { get; set; }

    [Required]
    public DateTime LicenseIssueDate { get; set; }

    [Required]
    public DateTime LicenseExpiryDate { get; set; }

    public long CivilId { get; set; }

    [StringLength(256)]
    public string ActivityType { get; set; }
}
