using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="Currency"/>.
/// </summary>
public class CurrencyDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int DecimalCount { get; set; }

    public string CurrencyShortFormEn { get; set; }

    public string CurrencyDenominationFormEn { get; set; }

    public string CurrencyShortFormAr { get; set; }

    public string CurrencyDenominationFormAr { get; set; }
}
