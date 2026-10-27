using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="PayrollCalendar"/>.
/// </summary>
public class PayrollCalendarDto : EntityDto<int>
{
    public int PayrollAreaId { get; set; }

    public string PayrollAreaName { get; set; }

    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
