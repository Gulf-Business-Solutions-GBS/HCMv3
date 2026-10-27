using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="DutyPeriodDetail"/>.
/// </summary>
public class DutyPeriodDetailDto : EntityDto<int>
{
    public int DutyPeriodId { get; set; }

    public string DutyPeriodName { get; set; }

    public int Week { get; set; }

    public int? Day1DutyId { get; set; }

    public int? Day2DutyId { get; set; }

    public int? Day3DutyId { get; set; }

    public int? Day4DutyId { get; set; }

    public int? Day5DutyId { get; set; }

    public int? Day6DutyId { get; set; }

    public int? Day7DutyId { get; set; }
}
