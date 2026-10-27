using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="CurrencyConvertor"/>.
/// </summary>
public class CurrencyConvertorDto : EntityDto<int>
{
    public DateTime CalendarDate { get; set; }

    public int? FromCurrId { get; set; }

    public string FromCurrName { get; set; }

    public int? ToCurrId { get; set; }

    public string ToCurrName { get; set; }

    public double Rate { get; set; }
}
