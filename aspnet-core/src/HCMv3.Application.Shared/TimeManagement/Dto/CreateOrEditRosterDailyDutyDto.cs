using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="RosterDailyDuty"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditRosterDailyDutyDto
{
    public int? Id { get; set; }

    [Required]
    public DateTime CalendarDate { get; set; }

    [Required]
    public int RosterDutyScheduleId { get; set; }

    [Required]
    public int DailyDutyId { get; set; }
}
