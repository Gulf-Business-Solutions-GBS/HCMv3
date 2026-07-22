using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="NumberSeries"/>.
/// </summary>
public class NumberSeriesDto : EntityDto<int>
{
    public string Code { get; set; }

    public string Name { get; set; }

    public long Start { get; set; }

    public string Prefix { get; set; }

    public string Formatting { get; set; }
}
