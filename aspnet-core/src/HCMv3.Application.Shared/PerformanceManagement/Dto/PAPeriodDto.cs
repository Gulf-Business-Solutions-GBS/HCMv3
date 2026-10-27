using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAPeriodDto : EntityDto<int>
{
    public string Name { get; set; }

    public bool Closed { get; set; }
}
