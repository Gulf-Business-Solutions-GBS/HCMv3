using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Create/Update input DTO for the combined Zero-<c>OrganizationUnit</c> + HR-profile screen.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditOrgUnitDto
{
    public long? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public long? ParentId { get; set; }

    [Required]
    public int OrganizationUnitTypeId { get; set; }

    public string Address { get; set; }

    public string OrgGeoLocation { get; set; }

    public int? PayrollAreaId { get; set; }

    /// <summary>
    /// The unit's logo (PNG, JPEG or GIF), kept on disk, never in the database. Null leaves the stored logo
    /// as it is -- so callers that know nothing about logos (Go-Live setup, the AI assistant) cannot wipe
    /// it by accident -- and new bytes replace it. To remove it, set <see cref="RemoveLogo"/>.
    /// </summary>
    [MaxLength(OrgUnitProfileConsts.MaxLogoBytes)]
    public byte[] Logo { get; set; }

    /// <summary>True removes the stored logo. Wins over <see cref="Logo"/>.</summary>
    public bool RemoveLogo { get; set; }
}
