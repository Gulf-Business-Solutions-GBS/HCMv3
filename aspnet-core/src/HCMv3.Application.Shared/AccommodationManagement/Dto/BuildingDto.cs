using Abp.Application.Services.Dto;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Building"/>.
/// </summary>
public class BuildingDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int LocationId { get; set; }

    public string LocationName { get; set; }

    public string Comments { get; set; }
}
