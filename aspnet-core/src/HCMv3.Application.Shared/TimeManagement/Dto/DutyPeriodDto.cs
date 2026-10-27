using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="DutyPeriod"/>.
/// </summary>
public class DutyPeriodDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? HolidayCalendarId { get; set; }

    public string HolidayCalendarName { get; set; }

    public int? SundayDutyId { get; set; }

    public int? MondayDutyId { get; set; }

    public int? TuesdayDutyId { get; set; }

    public int? WednesdayDutyId { get; set; }

    public int? ThursdayDutyId { get; set; }

    public int? FridayDutyId { get; set; }

    public int? SaturdayDutyId { get; set; }
}
