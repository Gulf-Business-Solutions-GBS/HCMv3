using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditTaskEventDto
{
    public int? Id { get; set; }

    public int? RefEmployeeId { get; set; }

    public int? NotificationConfigId { get; set; }

    [StringLength(256)]
    public string Subject { get; set; }

    public string Description { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public TaskEventStatus Status { get; set; }

    public int PercentCompleted { get; set; }

    public DateTime? DateCompleted { get; set; }

    public TimeSpan? RemindIn { get; set; }

    public DateTime? AlarmTime { get; set; }

    public int ObjectType { get; set; }

    public int ObjectOId { get; set; }
}
