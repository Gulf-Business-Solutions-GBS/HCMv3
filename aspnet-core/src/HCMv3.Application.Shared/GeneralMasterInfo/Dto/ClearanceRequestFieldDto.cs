using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="ClearanceRequestField"/>.
/// </summary>
public class ClearanceRequestFieldDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int? ClearanceRequestTypeId { get; set; }

    public string ClearanceRequestTypeName { get; set; }

    public bool IsActive { get; set; }
}
