using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Location"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditLocationDto
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

    public string Comments { get; set; }
}
