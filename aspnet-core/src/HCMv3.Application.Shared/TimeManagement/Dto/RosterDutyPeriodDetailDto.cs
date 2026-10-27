using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="RosterDutyPeriodDetail"/>.
/// </summary>
public class RosterDutyPeriodDetailDto : EntityDto<int>
{
    public int RosterDutyPeriodId { get; set; }

    public string RosterDutyPeriodName { get; set; }

    public int Week { get; set; }

    public int? Day1DutyId { get; set; }

    public int? Day2DutyId { get; set; }

    public int? Day3DutyId { get; set; }

    public int? Day4DutyId { get; set; }

    public int? Day5DutyId { get; set; }

    public int? Day6DutyId { get; set; }

    public int? Day7DutyId { get; set; }
}
