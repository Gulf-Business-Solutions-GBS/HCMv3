using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="BuildingContract"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditBuildingContractDto
{
    public int? Id { get; set; }

    [Required]
    public int BuildingId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string Comments { get; set; }
}
