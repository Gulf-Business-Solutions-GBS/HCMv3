using Abp.Application.Services.Dto;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Bed"/>.
/// </summary>
public class BedDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int RoomId { get; set; }

    public string RoomName { get; set; }

    public string Comments { get; set; }
}
