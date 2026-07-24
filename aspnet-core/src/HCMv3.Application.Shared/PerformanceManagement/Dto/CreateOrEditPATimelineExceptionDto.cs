using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPATimelineExceptionDto
{
    public int? Id { get; set; }

    [Required]
    public int PAPeriodId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public ActivityType ActivityType { get; set; }

    [Required]
    public DateTime ExtensionDate { get; set; }
}
