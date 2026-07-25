using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="IncidentClaim"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditIncidentClaimDto
{
    public int? Id { get; set; }

    [Required]
    public DateTime ClaimDate { get; set; }

    [Required]
    public int IncidentId { get; set; }

    public double Amount { get; set; }

    [Required]
    public int CurrencyId { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    public int? ClaimStatusId { get; set; }

    public string Comments { get; set; }
}
