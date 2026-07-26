using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditPunchNotificationLogDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeDutyPeriodId { get; set; }

    [StringLength(128)]
    public string Type { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public int? EmailNotificationLogId { get; set; }
}
