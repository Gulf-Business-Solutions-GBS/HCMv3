using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PAGoalDto : EntityDto<int>
{
    public int PAFormId { get; set; }

    public string Goal { get; set; }
}
