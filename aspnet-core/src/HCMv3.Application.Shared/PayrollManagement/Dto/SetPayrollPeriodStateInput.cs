using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>Moves one payroll area's control record to another state. See <see cref="PayrollPeriodState"/>.</summary>
public class SetPayrollPeriodStateInput
{
    /// <summary>The control record to move.</summary>
    [Required]
    public int Id { get; set; }

    [Required]
    public PayrollPeriodState State { get; set; }
}
