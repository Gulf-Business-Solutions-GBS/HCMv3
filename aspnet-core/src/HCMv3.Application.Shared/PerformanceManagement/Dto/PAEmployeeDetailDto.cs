using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAEmployeeDetailDto : EntityDto<int>
{
    public int PAEmployeeId { get; set; }

    public int KpiInfoId { get; set; }

    public string KpiInfoName { get; set; }

    public double Weight { get; set; }

    public string Target { get; set; }
}
