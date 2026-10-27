using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SimulationTimeRegisterDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditSimulationTimeRegisterDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int SimulationTimeRegisterId { get; set; }

    [Required]
    public int TimeTypeId { get; set; }

    public string TimeValue { get; set; }
}
