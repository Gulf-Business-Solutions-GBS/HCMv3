using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SimulationSalaryRegisterDetail"/>. <c>Id == null</c> means
/// create; otherwise update.
/// </summary>
public class CreateOrEditSimulationSalaryRegisterDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int SimulationSalaryRegisterId { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
