using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="PayrollControlRecord"/>.
/// </summary>
public class PayrollControlRecordDto : EntityDto<int>
{
    public int PayrollAreaId { get; set; }

    public string PayrollAreaName { get; set; }

    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public DateTime CalendarMonthStart { get; set; }

    public DateTime CalendarMonthEnd { get; set; }

    public bool PeriodLocked { get; set; }

    public bool PeriodClosed { get; set; }
}
