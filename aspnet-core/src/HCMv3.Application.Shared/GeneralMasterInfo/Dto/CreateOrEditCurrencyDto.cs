using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Currency"/>.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditCurrencyDto
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

    public int DecimalCount { get; set; }

    [StringLength(CurrencyConsts.MaxShortFormLength)]
    public string CurrencyShortFormEn { get; set; }

    [StringLength(CurrencyConsts.MaxDenominationFormLength)]
    public string CurrencyDenominationFormEn { get; set; }

    [StringLength(CurrencyConsts.MaxShortFormLength)]
    public string CurrencyShortFormAr { get; set; }

    [StringLength(CurrencyConsts.MaxDenominationFormLength)]
    public string CurrencyDenominationFormAr { get; set; }
}
