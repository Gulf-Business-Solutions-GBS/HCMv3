using System.ComponentModel.DataAnnotations;

namespace HCMv3.SystemSecurity.Dto;

public class CreateOrEditEmployeesNotificationDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int? TaskEventId { get; set; }

    public bool IsNotificationDeleted { get; set; }

    public bool IsNotificationRead { get; set; }
}
