using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SocialSecurityMaritalAllowance"/>. <c>Id == null</c> means
/// create; otherwise update.
/// </summary>
public class CreateOrEditSocialSecurityMaritalAllowanceDto
{
    public int? Id { get; set; }

    [Required]
    public int SocialSecurityId { get; set; }

    [Required]
    public int EducationBranchId { get; set; }

    [Required]
    public int MaritalStatusId { get; set; }

    public double Amount { get; set; }
}
