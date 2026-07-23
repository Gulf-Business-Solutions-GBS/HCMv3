using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="WorkLocationDutyPeriod"/>. <c>Id == null</c> means create;
/// otherwise update. <c>EndDate</c> is server-computed by the EndDate auto-chaining logic, so it is not
/// part of this input.
/// </summary>
public class CreateOrEditWorkLocationDutyPeriodDto
{
    public int? Id { get; set; }

    [Required]
    public int WorkLocationId { get; set; }

    [Required]
    public int DutyPeriodId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }
}
