using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Holiday"/>.
/// </summary>
public class HolidayDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int HolidayCalendarId { get; set; }

    public string HolidayCalendarName { get; set; }

    public int? OvertimeTypeId { get; set; }

    public string OvertimeTypeName { get; set; }
}
