using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class KpiByOrgUnitDto : EntityDto<int>
{
    public long OrganizationUnitId { get; set; }

    public string OrganizationUnitName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalWeight { get; set; }
}
