using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TempEmployeeBenefit"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditTempEmployeeBenefitDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int TempBenefitTypeId { get; set; }
}
