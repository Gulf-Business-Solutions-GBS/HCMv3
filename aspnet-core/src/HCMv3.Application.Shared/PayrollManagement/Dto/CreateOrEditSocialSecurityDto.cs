using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SocialSecurity"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditSocialSecurityDto
{
    public int? Id { get; set; }

    [Required]
    public int NationalityId { get; set; }

    public double EarningsCap { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public double ChildAllowance { get; set; }

    public double SpecialNeedChildAllowance { get; set; }
}
