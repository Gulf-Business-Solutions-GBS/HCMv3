using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditKpiByJobTitleDto
{
    public int? Id { get; set; }

    [Required]
    public int JobTitleId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }
}
