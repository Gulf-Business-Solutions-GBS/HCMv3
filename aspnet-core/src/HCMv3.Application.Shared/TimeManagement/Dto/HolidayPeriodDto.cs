using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="HolidayPeriod"/>.
/// </summary>
public class HolidayPeriodDto : EntityDto<int>
{
    public int HolidayId { get; set; }

    public string HolidayName { get; set; }

    public DateTime HolidayDate { get; set; }
}
