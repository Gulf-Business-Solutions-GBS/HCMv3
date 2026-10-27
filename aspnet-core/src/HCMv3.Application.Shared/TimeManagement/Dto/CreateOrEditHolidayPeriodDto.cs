using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="HolidayPeriod"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditHolidayPeriodDto
{
    public int? Id { get; set; }

    [Required]
    public int HolidayId { get; set; }

    [Required]
    public DateTime HolidayDate { get; set; }
}
