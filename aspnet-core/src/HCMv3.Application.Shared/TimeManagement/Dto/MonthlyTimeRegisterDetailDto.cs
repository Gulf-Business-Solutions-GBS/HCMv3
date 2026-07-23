using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="MonthlyTimeRegisterDetail"/>.
/// </summary>
public class MonthlyTimeRegisterDetailDto : EntityDto<int>
{
    public int MonthlyTimeRegisterId { get; set; }

    public int TimeTypeId { get; set; }

    public string TimeTypeName { get; set; }

    public string TimeValue { get; set; }
}
