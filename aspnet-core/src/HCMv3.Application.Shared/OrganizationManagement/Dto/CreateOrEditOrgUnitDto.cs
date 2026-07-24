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
}
