using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CostRegister"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditCostRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime Period { get; set; }

    public PayrollRunType PayrollRunType { get; set; }
}
