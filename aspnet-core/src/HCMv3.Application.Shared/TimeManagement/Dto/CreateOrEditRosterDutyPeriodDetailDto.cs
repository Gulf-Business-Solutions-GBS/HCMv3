using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="RosterDutyPeriodDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditRosterDutyPeriodDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int RosterDutyPeriodId { get; set; }

    [Required]
    public int Week { get; set; }

    public int? Day1DutyId { get; set; }

    public int? Day2DutyId { get; set; }

    public int? Day3DutyId { get; set; }

    public int? Day4DutyId { get; set; }

    public int? Day5DutyId { get; set; }

    public int? Day6DutyId { get; set; }

    public int? Day7DutyId { get; set; }
}
