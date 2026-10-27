using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPATimelineDto
{
    public int? Id { get; set; }

    [Required]
    public int PAPeriodId { get; set; }

    [Required]
    public ActivityType ActivityType { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
