using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="DisciplinaryDeductionType"/>.
/// </summary>
public class DisciplinaryDeductionTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public DisciplinaryDeductionUnit DisciplinaryDeductionUnit { get; set; }

    public double DeductionValue { get; set; }
}
