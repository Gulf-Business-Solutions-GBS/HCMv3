using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class EmployeesNotificationLogDto : EntityDto<int>
{
    public int? EmployeeId { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime DismissTime { get; set; }
    public int? TaskEventId { get; set; }
    public string TaskEventSubject { get; set; }
    public int? EmailNotificationLogId { get; set; }
    public string NotificationType { get; set; }
    public DateTime Date { get; set; }
}
