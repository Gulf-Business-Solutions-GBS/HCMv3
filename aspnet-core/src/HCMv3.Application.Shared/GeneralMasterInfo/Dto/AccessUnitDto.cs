using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="AccessUnit"/>.
/// </summary>
public class AccessUnitDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string IPAddress { get; set; }

    public int Locked { get; set; }
}
