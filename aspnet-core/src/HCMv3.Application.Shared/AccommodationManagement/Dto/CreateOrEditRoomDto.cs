using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Room"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditRoomDto
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
    public int FlatId { get; set; }

    public string Comments { get; set; }
}
