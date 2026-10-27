using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Duty"/>.
/// </summary>
public class DutyDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public int? BreakShiftId { get; set; }

    public string BreakShiftName { get; set; }

    public int? OvertimeTypeId { get; set; }

    public string OvertimeTypeName { get; set; }

    public bool AllowTransStart { get; set; }

    public bool AllowTransEnd { get; set; }

    public bool IsNightShift { get; set; }

    public bool IsWeeklyOff { get; set; }

    public bool IsRestDay { get; set; }

    public bool IsHalfDay { get; set; }

    public TimeSpan? FlexiStartTime { get; set; }

    public TimeSpan? FlexiEndTime { get; set; }

    public bool IsActive { get; set; }
}
