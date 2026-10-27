using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroupBenefit"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeSubGroupBenefitDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSubGroupId { get; set; }

    [Required]
    public int BenefitTypeId { get; set; }

    public double MaxAmount { get; set; }
}
