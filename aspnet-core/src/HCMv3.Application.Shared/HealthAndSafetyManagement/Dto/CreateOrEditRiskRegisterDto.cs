using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="RiskRegister"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditRiskRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public DateTime ReportingDate { get; set; }

    public DateTime ReportingTime { get; set; }

    [Required]
    public int RiskTypeId { get; set; }

    public string Description { get; set; }

    [Required]
    public int ImpactId { get; set; }

    [Required]
    public int PossibilityId { get; set; }

    [Required]
    public int ReportedById { get; set; }

    [Required]
    public int LocationId { get; set; }

    public int? AssignedToId { get; set; }

    public string RiskMitigation { get; set; }

    public int? ActionId { get; set; }

    public int? StatusId { get; set; }

    public DateTime ReassessmentDate { get; set; }
}
