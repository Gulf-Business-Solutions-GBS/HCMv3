using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PATimelineDto : EntityDto<int>
{
    public int PAPeriodId { get; set; }

    public string PAPeriodName { get; set; }

    public ActivityType ActivityType { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
