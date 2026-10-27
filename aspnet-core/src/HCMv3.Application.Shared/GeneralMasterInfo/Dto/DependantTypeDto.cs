using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="DependantType"/>.
/// </summary>
public class DependantTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public Gender Gender { get; set; }

    public bool IsEmployee { get; set; }
}
