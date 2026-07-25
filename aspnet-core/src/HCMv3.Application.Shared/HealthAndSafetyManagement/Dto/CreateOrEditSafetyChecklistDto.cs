using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SafetyChecklist"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditSafetyChecklistDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    public int SafetyAreaAssetId { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; set; }
}
