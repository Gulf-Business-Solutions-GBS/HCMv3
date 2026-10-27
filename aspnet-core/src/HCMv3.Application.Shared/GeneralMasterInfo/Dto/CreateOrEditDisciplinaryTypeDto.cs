using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="DisciplinaryType"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditDisciplinaryTypeDto
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

    public int RepeatDurationInDays { get; set; }

    /// <summary>
    /// Legacy <c>ViolationType.WageType</c>: one of <c>GetViolationWageTypes</c>. Optional (legacy required it;
    /// older rows have none).
    /// </summary>
    public int? WageTypeId { get; set; }
}
