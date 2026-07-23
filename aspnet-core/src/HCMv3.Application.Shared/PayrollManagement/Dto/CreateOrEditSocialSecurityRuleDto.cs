using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SocialSecurityRule"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditSocialSecurityRuleDto
{
    public int? Id { get; set; }

    [Required]
    public int SocialSecurityId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public int QualificationId { get; set; }

    public Gender Gender { get; set; }

    public int? MaritalStatusId { get; set; }

    public int Dependant { get; set; }

    public bool Disability { get; set; }

    public double Amount { get; set; }
}
