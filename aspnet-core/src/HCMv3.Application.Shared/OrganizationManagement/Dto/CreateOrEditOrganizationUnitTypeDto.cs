using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OrganizationUnitType"/>.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditOrganizationUnitTypeDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Must be one of <see cref="OrganizationUnitType.AllowedCodes"/> — selected from a dropdown, not
    /// auto-generated.
    /// </summary>
    [Required]
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public bool AllowLeaveConflict { get; set; }

    public bool AssignPayrollArea { get; set; }
}
