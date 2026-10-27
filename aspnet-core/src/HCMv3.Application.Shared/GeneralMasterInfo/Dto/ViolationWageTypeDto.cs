using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// One wage type a disciplinary (violation) type may deduct through -- legacy
/// <c>DisciplinaryType.ViolationWageType</c>.
/// </summary>
public class ViolationWageTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
