using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CostCenter"/>.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditCostCenterDto
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

    public int? CostCenterGroupId { get; set; }
}
