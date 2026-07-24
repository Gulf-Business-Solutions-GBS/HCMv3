using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TravelFeedback"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditTravelFeedbackDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int? TravelId { get; set; }

    public DateTime ClaimDate { get; set; }

    public double ClaimAmount { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
