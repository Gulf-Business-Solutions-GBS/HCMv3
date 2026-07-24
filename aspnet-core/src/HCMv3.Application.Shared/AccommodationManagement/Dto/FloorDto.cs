using Abp.Application.Services.Dto;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Floor"/>.
/// </summary>
public class FloorDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int BuildingId { get; set; }

    public string BuildingName { get; set; }

    public string Comments { get; set; }
}
