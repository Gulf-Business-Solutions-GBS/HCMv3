using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class TaskEventDto : EntityDto<int>
{
    public int? RefEmployeeId { get; set; }
    public string RefEmployeeFullNameEn { get; set; }
    public int? NotificationConfigId { get; set; }
    public string NotificationConfigName { get; set; }
    public string Subject { get; set; }
    public string Description { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime StartDate { get; set; }
    public TaskEventStatus Status { get; set; }
    public int PercentCompleted { get; set; }
    public DateTime? DateCompleted { get; set; }
    public TimeSpan? RemindIn { get; set; }
    public DateTime? AlarmTime { get; set; }
    public int ObjectType { get; set; }
    public int ObjectOId { get; set; }
}
