using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="RosterDutySchedule"/>.
/// </summary>
public class RosterDutyScheduleDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public DateTime StartDate { get; set; }

    public int DutyPeriodId { get; set; }

    public string DutyPeriodName { get; set; }

    public int StartPoint { get; set; }

    public int? HolidayCalendarId { get; set; }

    public string HolidayCalendarName { get; set; }
}
