using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SimulationTimeRegisterDetail"/>.
/// </summary>
public class SimulationTimeRegisterDetailDto : EntityDto<int>
{
    public int SimulationTimeRegisterId { get; set; }

    public int TimeTypeId { get; set; }

    public string TimeTypeName { get; set; }

    public string TimeValue { get; set; }
}
