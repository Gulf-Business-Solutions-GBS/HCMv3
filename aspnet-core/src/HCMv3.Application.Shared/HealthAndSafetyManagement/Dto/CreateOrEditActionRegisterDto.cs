using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ActionRegister"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditActionRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public DateTime ActionDate { get; set; }

    public string Description { get; set; }

    [Required]
    public int IncidentId { get; set; }

    [Required]
    public int RiskId { get; set; }

    [Required]
    public int AssignedToId { get; set; }

    public int? StatusId { get; set; }
}
