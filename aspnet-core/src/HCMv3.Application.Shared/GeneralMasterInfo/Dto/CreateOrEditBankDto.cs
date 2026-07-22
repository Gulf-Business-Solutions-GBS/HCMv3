using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Bank"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditBankDto
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

    [StringLength(256)]
    public string ExternalCode { get; set; }

    [StringLength(256)]
    public string SWIFTCode { get; set; }

    [StringLength(256)]
    public string CompanyCode { get; set; }

    public int? BankFileId { get; set; }
}
