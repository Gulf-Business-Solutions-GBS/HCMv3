using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SimulationSalaryRegister"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditSimulationSalaryRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime ForPeriod { get; set; }

    [Required]
    public DateTime InPeriod { get; set; }

    public PayrollRunType PayrollRunType { get; set; }

    public PayrollResultType PayrollResultType { get; set; }

    [Required]
    public int PayrollReasonTypeId { get; set; }
}
