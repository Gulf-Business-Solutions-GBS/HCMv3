using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="IncidentRegister"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditIncidentRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public DateTime IncidentDate { get; set; }

    public DateTime IncidentTime { get; set; }

    [Required]
    public int IncidentTypeId { get; set; }

    public string Description { get; set; }

    [Required]
    public int NatureId { get; set; }

    [Required]
    public int SeverityId { get; set; }

    public int? ReportedById { get; set; }

    public int? PersonInvolvedId { get; set; }

    public string OtherPeopleInvolved { get; set; }

    public int? LocationId { get; set; }

    public int? AssignedToId { get; set; }

    public string DisabilityPercentage { get; set; }

    public int? StatusId { get; set; }

    public string Comments { get; set; }
}
