using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="YearlyTimeRegisterDetail"/>.
/// </summary>
public class YearlyTimeRegisterDetailDto : EntityDto<int>
{
    public int YearlyTimeRegisterId { get; set; }

    public int TimeTypeId { get; set; }

    public string TimeTypeName { get; set; }

    public string TimeValue { get; set; }
}
