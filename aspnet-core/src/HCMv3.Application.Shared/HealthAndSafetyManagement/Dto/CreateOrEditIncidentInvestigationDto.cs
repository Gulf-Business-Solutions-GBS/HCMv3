using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="IncidentInvestigation"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditIncidentInvestigationDto
{
    public int? Id { get; set; }

    [Required]
    public DateTime InvestigationDate { get; set; }

    public DateTime InvestigationTime { get; set; }

    [Required]
    public int IncidentId { get; set; }

    [Required]
    public int RootCauseId { get; set; }

    public string RootCauseAnalysis { get; set; }

    public string Comments { get; set; }
}
