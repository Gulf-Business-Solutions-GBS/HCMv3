using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SimulationTimeRegister"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditSimulationTimeRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime DayDate { get; set; }

    public int? DutyPeriodId { get; set; }
}
