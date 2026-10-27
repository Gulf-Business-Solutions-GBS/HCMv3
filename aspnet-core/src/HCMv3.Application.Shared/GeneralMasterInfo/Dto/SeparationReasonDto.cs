using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="SeparationReason"/>.
/// </summary>
public class SeparationReasonDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public bool IsSystemRecord { get; set; }

    public bool AllowAdvanceEoS { get; set; }
}
