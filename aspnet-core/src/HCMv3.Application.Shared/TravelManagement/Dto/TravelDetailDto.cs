using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TravelDetail"/>.
/// </summary>
public class TravelDetailDto : EntityDto<int>
{
    public int TravelId { get; set; }

    public int DepartureId { get; set; }

    public string DepartureNameEn { get; set; }

    public int ArrivalId { get; set; }

    public string ArrivalNameEn { get; set; }

    public DateTime DepartureDate { get; set; }

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalDate { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int? HotelId { get; set; }

    public string HotelNameEn { get; set; }

    public int? TravelAirlineId { get; set; }

    public string TravelAirlineNameEn { get; set; }

    public bool NightStay { get; set; }

    public int TotalNights { get; set; }

    public double TotalAmount { get; set; }

    public string Comments { get; set; }
}
