using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="ClearanceRequestFieldDetail"/>.
/// </summary>
public class ClearanceRequestFieldDetailDto : EntityDto<int>
{
    public int? ClearanceRequestFieldId { get; set; }

    public string ClearanceRequestFieldName { get; set; }
}
