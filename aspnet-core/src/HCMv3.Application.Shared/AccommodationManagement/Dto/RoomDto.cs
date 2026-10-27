using Abp.Application.Services.Dto;

namespace HCMv3.AccommodationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Room"/>.
/// </summary>
public class RoomDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int FlatId { get; set; }

    public string FlatName { get; set; }

    public string Comments { get; set; }
}
