using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SimulationSalaryRegisterDaysDetail"/>. <c>Id == null</c> means
/// create; otherwise update.
/// </summary>
public class CreateOrEditSimulationSalaryRegisterDaysDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int SimulationSalaryRegisterDetailId { get; set; }

    [Required]
    public DateTime Period { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
