using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SocialSecurityDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditSocialSecurityDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int SocialSecurityId { get; set; }

    public double MinEarnings { get; set; }

    public double MaxEarnings { get; set; }

    public double EmployeeContribution { get; set; }

    public double EmployerContribution { get; set; }

    public double MinEarningsEmployeeContribution { get; set; }
}
