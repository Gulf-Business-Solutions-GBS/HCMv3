using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Shared create/update input reused by every pure bilingual lookup app service.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditSimpleLookupDto
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
}
