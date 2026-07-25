using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.BenefitsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeInsuranceDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeInsuranceDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeInsuranceId { get; set; }

    [Required]
    public DateTime InstallmentDate { get; set; }

    [Range(0.01, 999999999)]
    public double InstallmentAmount { get; set; }

    public InstallmentType InstallmentType { get; set; }
}
