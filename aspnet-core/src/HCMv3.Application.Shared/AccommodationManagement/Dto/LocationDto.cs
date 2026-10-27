using Abp.Application.Services.Dto;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Location"/>.
/// </summary>
public class LocationDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string Comments { get; set; }
}
