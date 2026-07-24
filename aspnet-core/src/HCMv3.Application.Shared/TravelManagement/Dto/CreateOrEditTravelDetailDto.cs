using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TravelDetail"/>. <c>Id == null</c> means create; otherwise update.
/// <see cref="TravelDetail.TotalAmount"/> is intentionally NOT settable here -- see that entity's remarks.
/// </summary>
public class CreateOrEditTravelDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int TravelId { get; set; }

    [Required]
    public int DepartureId { get; set; }

    [Required]
    public int ArrivalId { get; set; }

    [Required]
    public DateTime DepartureDate { get; set; }

    public DateTime DepartureTime { get; set; }

    [Required]
    public DateTime ArrivalDate { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int? HotelId { get; set; }

    public int? TravelAirlineId { get; set; }

    public bool NightStay { get; set; }

    public int TotalNights { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
