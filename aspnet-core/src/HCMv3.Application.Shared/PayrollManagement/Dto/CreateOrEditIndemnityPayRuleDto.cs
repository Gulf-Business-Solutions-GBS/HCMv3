using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="IndemnityPayRule"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditIndemnityPayRuleDto
{
    public int? Id { get; set; }

    [Required]
    public PersonnelAdministration.SeparationType SeparationType { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public double MaxNumber { get; set; }

    public Unit MaxUnit { get; set; }

    public double MaxPayPercentage { get; set; }

    public int? CountryId { get; set; }
}
