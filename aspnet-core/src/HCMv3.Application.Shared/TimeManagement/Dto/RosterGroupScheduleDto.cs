using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="RosterGroupSchedule"/>.
/// </summary>
public class RosterGroupScheduleDto : EntityDto<int>
{
    public int RosterGroupId { get; set; }

    public string RosterGroupName { get; set; }

    public int DutyPeriodId { get; set; }

    public string DutyPeriodName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int StartPoint { get; set; }
}
