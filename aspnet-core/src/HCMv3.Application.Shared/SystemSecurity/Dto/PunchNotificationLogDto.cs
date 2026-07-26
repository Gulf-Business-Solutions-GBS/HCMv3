using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class PunchNotificationLogDto : EntityDto<int>
{
    public int EmployeeDutyPeriodId { get; set; }
    public string Type { get; set; }
    public DateTime Date { get; set; }
    public int? EmailNotificationLogId { get; set; }
}
