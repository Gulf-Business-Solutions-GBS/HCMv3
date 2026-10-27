using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SalaryRegisterDaysDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditSalaryRegisterDaysDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int SalaryRegisterDetailId { get; set; }

    [Required]
    public DateTime Period { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
