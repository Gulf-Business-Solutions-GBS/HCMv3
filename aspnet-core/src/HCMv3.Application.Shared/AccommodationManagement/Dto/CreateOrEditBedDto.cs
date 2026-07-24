using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Bed"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditBedDto
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
    public int RoomId { get; set; }

    public string Comments { get; set; }
}
