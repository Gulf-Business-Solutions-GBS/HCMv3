using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="RosterDailyDuty"/>.
/// </summary>
public class RosterDailyDutyDto : EntityDto<int>
{
    public DateTime CalendarDate { get; set; }

    public int RosterDutyScheduleId { get; set; }

    public string RosterDutyScheduleName { get; set; }

    public int DailyDutyId { get; set; }

    public string DailyDutyName { get; set; }
}
