using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Floor"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditFloorDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    [Required]
    public int BuildingId { get; set; }

    public string Comments { get; set; }
}
