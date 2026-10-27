using System.ComponentModel.DataAnnotations;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TravelPerDiem"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditTravelPerDiemDto
{
    public int? Id { get; set; }

    [Required]
    public int FromDestinationId { get; set; }

    [Required]
    public int ToDestinationId { get; set; }

    [Required]
    public int EmployeeGroupId { get; set; }

    [Required]
    public int EmployeeSubGroupId { get; set; }

    [Range(0, 999999999)]
    public double Amount { get; set; }
}
