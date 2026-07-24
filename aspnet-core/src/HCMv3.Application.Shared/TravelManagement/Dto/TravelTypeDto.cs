using Abp.Application.Services.Dto;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TravelType"/>.
/// </summary>
public class TravelTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int? WageTypeId { get; set; }

    public string WageTypeNameEn { get; set; }
}
