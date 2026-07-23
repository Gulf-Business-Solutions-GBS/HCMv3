using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="WorkLocationDutyPeriod"/>.
/// </summary>
public class WorkLocationDutyPeriodDto : EntityDto<int>
{
    public int WorkLocationId { get; set; }

    public string WorkLocationName { get; set; }

    public int DutyPeriodId { get; set; }

    public string DutyPeriodName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
