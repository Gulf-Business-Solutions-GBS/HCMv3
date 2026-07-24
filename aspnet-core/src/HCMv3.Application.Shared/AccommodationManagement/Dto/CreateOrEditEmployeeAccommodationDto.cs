using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeAccommodation"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeAccommodationDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public int AccommodationTypeId { get; set; }

    public double DeductionAmount { get; set; }

    [Required]
    public int LocationId { get; set; }

    [Required]
    public int BuildingId { get; set; }

    [Required]
    public int FloorId { get; set; }

    [Required]
    public int FlatId { get; set; }

    [Required]
    public int RoomId { get; set; }

    [Required]
    public int BedId { get; set; }

    public string Comments { get; set; }
}
