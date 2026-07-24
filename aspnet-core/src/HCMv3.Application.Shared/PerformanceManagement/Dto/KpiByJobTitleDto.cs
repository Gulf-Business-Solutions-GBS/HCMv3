using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class KpiByJobTitleDto : EntityDto<int>
{
    public int JobTitleId { get; set; }

    public string JobTitleName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalWeight { get; set; }
}
