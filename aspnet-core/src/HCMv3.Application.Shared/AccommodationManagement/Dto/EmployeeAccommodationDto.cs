using System;
using Abp.Application.Services.Dto;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeAccommodation"/>.
/// </summary>
public class EmployeeAccommodationDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int AccommodationTypeId { get; set; }

    public string AccommodationTypeName { get; set; }

    public double DeductionAmount { get; set; }

    public int LocationId { get; set; }

    public string LocationName { get; set; }

    public int BuildingId { get; set; }

    public string BuildingName { get; set; }

    public int FloorId { get; set; }

    public string FloorName { get; set; }

    public int FlatId { get; set; }

    public string FlatName { get; set; }

    public int RoomId { get; set; }

    public string RoomName { get; set; }

    public int BedId { get; set; }

    public string BedName { get; set; }

    public string Comments { get; set; }
}
