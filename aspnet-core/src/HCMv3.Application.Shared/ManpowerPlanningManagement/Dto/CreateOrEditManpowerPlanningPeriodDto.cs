using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ManpowerPlanningPeriod"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditManpowerPlanningPeriodDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    public bool Active { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
