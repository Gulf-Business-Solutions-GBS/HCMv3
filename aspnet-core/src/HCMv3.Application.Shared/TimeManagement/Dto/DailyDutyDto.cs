using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="DailyDuty"/>.
/// </summary>
public class DailyDutyDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public bool IsNightShift { get; set; }

    public int? BreakShiftId { get; set; }

    public string BreakShiftName { get; set; }

    public bool AllowTransStart { get; set; }

    public bool AllowTransEnd { get; set; }
}
