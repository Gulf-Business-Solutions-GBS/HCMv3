using System;
using Abp.Application.Services.Dto;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="BuildingContract"/>.
/// </summary>
public class BuildingContractDto : EntityDto<int>
{
    public int BuildingId { get; set; }

    public string BuildingName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }
}
