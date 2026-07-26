using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditEmployeesNotificationLogDto
{
    public int? Id { get; set; }

    public int? EmployeeId { get; set; }

    public DateTime DismissTime { get; set; }

    public int? TaskEventId { get; set; }

    public int? EmailNotificationLogId { get; set; }

    [StringLength(128)]
    public string NotificationType { get; set; }

    [Required]
    public DateTime Date { get; set; }
}
