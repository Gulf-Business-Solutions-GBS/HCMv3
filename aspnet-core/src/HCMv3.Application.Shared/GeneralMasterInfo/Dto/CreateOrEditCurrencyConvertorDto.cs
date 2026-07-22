using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CurrencyConvertor"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditCurrencyConvertorDto
{
    public int? Id { get; set; }

    [Required]
    public DateTime CalendarDate { get; set; }

    [Required]
    public int? FromCurrId { get; set; }

    [Required]
    public int? ToCurrId { get; set; }

    public double Rate { get; set; }
}
