using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Sponsor"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditSponsorDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code.
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    [StringLength(256)]
    [EmailAddress]
    public string Email { get; set; }

    public Gender Gender { get; set; }

    [StringLength(256)]
    public string Profession { get; set; }

    [StringLength(256)]
    public string Building { get; set; }

    [StringLength(256)]
    public string Street { get; set; }

    [StringLength(256)]
    public string Area { get; set; }

    [StringLength(256)]
    public string Block { get; set; }

    public int Telephone { get; set; }

    [Required]
    public int NationalityId { get; set; }

    [Required]
    public int BusinessTypeId { get; set; }

    [Required]
    public long OrganizationUnitId { get; set; }

    public long UnifiedNum { get; set; }

    public long SocialSecurityNum { get; set; }

    public long MOSALID { get; set; }

    [Required]
    public int GovernorateId { get; set; }

    [StringLength(256)]
    public string BusinessOwnerNameEn { get; set; }

    [StringLength(256)]
    public string BusinessOwnerNameAr { get; set; }

    [StringLength(50)]
    public string Fax { get; set; }

    /// <summary>Labeled "PACI Number" in the legacy UI.</summary>
    [StringLength(50)]
    public string POBox { get; set; }
}
